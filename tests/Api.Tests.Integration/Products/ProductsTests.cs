using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Dtos;
using Domain.Categories;
using Domain.Products;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;
using Tests.Data.Products;

namespace Api.Tests.Integration.Products;

// Сценарії розд. 11 ТЗ для товарів: список, деталі, створення з перевіркою
// категорій і сповіщенням, дублікат, валідація, PUT із пересинхронізацією
// категорій у транзакції та DELETE.
public sealed class ProductsTests(IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedAndPersistsProduct()
    {
        var category = await SeedCategoryAsync();

        var request = new
        {
            title = ProductsData.UniqueTitle("Fender Telecaster"),
            description = "Electric guitar, ash body, 21 frets.",
            categoryIds = new[] { category.Id.Value },
            specs = new
            {
                brand = "Fender",
                model = "Telecaster",
                color = "Butterscotch",
                condition = "New",
                manufactureYear = 2023,
            },
        };

        var response = await Client.PostAsJsonAsync("/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<ProductDetailsDto>();
        created.Should().NotBeNull();
        created!.Title.Should().Be(request.title);
        created.Description.Should().Be(request.description);
        created.Specs.Brand.Should().Be("Fender");
        created.Categories.Should().ContainSingle()
            .Which.Id.Should().Be(category.Id.Value);
        created.Images.Should().BeEmpty();
        response.Headers.Location!.ToString().Should().Contain(created.Id.ToString());

        var persisted = await DbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == new ProductId(created.Id));

        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be(request.title);
        persisted.Description.Should().Be(request.description);
        persisted.Specs.Should().Be(new ProductSpecs("Fender", "Telecaster", "Butterscotch", "New", 2023));

        var links = await DbContext.ProductCategories
            .AsNoTracking()
            .Where(link => link.ProductId == new ProductId(created.Id))
            .ToListAsync();

        links.Should().ContainSingle()
            .Which.CategoryId.Should().Be(category.Id);

        Fixture.NotificationService.WasSentFor(new ProductId(created.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task Create_WithUnknownCategory_ReturnsNotFoundWithMissingIds()
    {
        var missingCategoryId = Guid.NewGuid();

        var request = new
        {
            title = ProductsData.UniqueTitle("Unknown category product"),
            description = "Product that references a category which does not exist.",
            categoryIds = new[] { missingCategoryId },
            specs = (object?)null,
        };

        var response = await Client.PostAsJsonAsync("/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Title.Should().Be("One or more categories were not found");

        var reportedIds = ((JsonElement)problem.Extensions["missingCategoryIds"]!)
            .EnumerateArray()
            .Select(element => element.GetGuid())
            .ToList();

        reportedIds.Should().Contain(missingCategoryId);

        var persisted = await DbContext.Products
            .AsNoTracking()
            .AnyAsync(product => product.Title == request.title);

        persisted.Should().BeFalse();
        Fixture.NotificationService.SentProductIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithDuplicateTitle_ReturnsConflict()
    {
        var category = await SeedCategoryAsync();
        var product = await SeedProductAsync(category);

        var request = new
        {
            title = product.Title,
            description = "Another description, but the same title.",
            categoryIds = new[] { category.Id.Value },
            specs = (object?)null,
        };

        var response = await Client.PostAsJsonAsync("/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status409Conflict);

        Fixture.NotificationService.SentProductIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsValidationProblem()
    {
        var request = new
        {
            title = "ab",
            description = "",
            categoryIds = Array.Empty<Guid>(),
            specs = (object?)null,
        };

        var response = await Client.PostAsJsonAsync("/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey("Title");
        problem.Errors.Should().ContainKey("Description");
        problem.Errors.Should().ContainKey("CategoryIds");

        Fixture.NotificationService.SentProductIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsProductsWithCategoriesAndNewestFirst()
    {
        var category = await SeedCategoryAsync();

        var older = ProductsData.MainProduct(category);
        await Task.Delay(25);
        var newer = ProductsData.SecondProduct(category);

        DbContext.Products.AddRange(older, newer);
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var products = await response.Content.ReadFromJsonAsync<List<ProductListItemDto>>();
        products.Should().NotBeNull();

        var olderIndex = products!.FindIndex(product => product.Id == older.Id.Value);
        var newerIndex = products.FindIndex(product => product.Id == newer.Id.Value);

        olderIndex.Should().BeGreaterThanOrEqualTo(0);
        newerIndex.Should().BeGreaterThanOrEqualTo(0);
        newerIndex.Should().BeLessThan(olderIndex);

        products[olderIndex].Categories.Should().ContainSingle()
            .Which.Id.Should().Be(category.Id.Value);
        products[newerIndex].Categories.Should().ContainSingle()
            .Which.Id.Should().Be(category.Id.Value);

        // Список відсортований за CreatedAt desc; рівні мітки часу не заборонені.
        for (var index = 1; index < products.Count; index++)
        {
            products[index - 1].CreatedAt.Should().BeOnOrAfter(products[index].CreatedAt);
        }
    }

    [Fact]
    public async Task GetById_WithExistingProduct_ReturnsDetails()
    {
        var category = await SeedCategoryAsync();
        var product = await SeedProductAsync(category);

        var response = await Client.GetAsync($"/products/{product.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await response.Content.ReadFromJsonAsync<ProductDetailsDto>();
        details.Should().NotBeNull();
        details!.Id.Should().Be(product.Id.Value);
        details.Title.Should().Be(product.Title);
        details.Specs.Should().Be(ProductSpecsDto.FromDomainModel(product.Specs));
        details.Categories.Should().ContainSingle()
            .Which.Name.Should().Be(category.Name);
        details.Images.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Update_ReplacesFieldsAndResyncsCategories()
    {
        var firstCategory = await SeedCategoryAsync();
        var secondCategory = await SeedCategoryAsync();
        var product = await SeedProductAsync(firstCategory);

        var request = new
        {
            id = product.Id.Value,
            title = ProductsData.UniqueTitle("Updated Stratocaster"),
            description = "Updated description for the guitar.",
            categoryIds = new[] { secondCategory.Id.Value },
            specs = new
            {
                brand = "Squier",
                model = "Affinity Stratocaster",
                color = "Black",
                condition = "Used",
                manufactureYear = 2021,
            },
        };

        var response = await Client.PutAsJsonAsync("/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<ProductDetailsDto>();
        updated.Should().NotBeNull();
        updated!.Title.Should().Be(request.title);
        updated.Description.Should().Be(request.description);
        updated.Specs.Brand.Should().Be("Squier");
        updated.Categories.Should().ContainSingle()
            .Which.Id.Should().Be(secondCategory.Id.Value);
        updated.UpdatedAt.Should().NotBeNull();

        var persisted = await DbContext.Products
            .AsNoTracking()
            .SingleAsync(item => item.Id == product.Id);

        persisted.Title.Should().Be(request.title);
        persisted.Description.Should().Be(request.description);
        persisted.Specs.Should().Be(new ProductSpecs("Squier", "Affinity Stratocaster", "Black", "Used", 2021));
        persisted.UpdatedAt.Should().NotBeNull();

        var links = await DbContext.ProductCategories
            .AsNoTracking()
            .Where(link => link.ProductId == product.Id)
            .ToListAsync();

        links.Should().ContainSingle()
            .Which.CategoryId.Should().Be(secondCategory.Id);
    }

    [Fact]
    public async Task Update_WithUnknownCategory_ReturnsNotFoundAndKeepsProduct()
    {
        var category = await SeedCategoryAsync();
        var product = await SeedProductAsync(category);
        var originalTitle = product.Title;

        var request = new
        {
            id = product.Id.Value,
            title = ProductsData.UniqueTitle("Should not be saved"),
            description = "This update must be rolled back.",
            categoryIds = new[] { Guid.NewGuid() },
            specs = (object?)null,
        };

        var response = await Client.PutAsJsonAsync("/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var persisted = await DbContext.Products
            .AsNoTracking()
            .SingleAsync(item => item.Id == product.Id);

        persisted.Title.Should().Be(originalTitle);
    }

    [Fact]
    public async Task Update_WithUnknownProduct_ReturnsNotFound()
    {
        var category = await SeedCategoryAsync();

        var request = new
        {
            id = Guid.NewGuid(),
            title = ProductsData.UniqueTitle("Unknown product"),
            description = "There is no product with this id.",
            categoryIds = new[] { category.Id.Value },
            specs = (object?)null,
        };

        var response = await Client.PutAsJsonAsync("/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Delete_RemovesProductAndCategoryLinks()
    {
        var category = await SeedCategoryAsync();
        var product = await SeedProductAsync(category);

        var response = await Client.DeleteAsync($"/products/{product.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var productExists = await DbContext.Products
            .AsNoTracking()
            .AnyAsync(item => item.Id == product.Id);

        productExists.Should().BeFalse();

        var linksExist = await DbContext.ProductCategories
            .AsNoTracking()
            .AnyAsync(link => link.ProductId == product.Id);

        linksExist.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WithUnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    private async Task<Category> SeedCategoryAsync()
    {
        var category = CategoriesData.MainCategory();
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        return category;
    }

    private async Task<Product> SeedProductAsync(params Category[] categories)
    {
        var product = ProductsData.MainProduct(categories);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        return product;
    }
}
