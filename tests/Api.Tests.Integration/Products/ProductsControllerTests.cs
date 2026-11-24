using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Dtos;
using Domain.Categories;
using Domain.Products;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;
using Tests.Data.ProductImages;
using Tests.Data.Products;
using Xunit;

namespace Api.Tests.Integration.Products;

public class ProductsControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Category _firstTestCategory;
    private readonly Category _secondTestCategory;
    private readonly Product _firstTestProduct;
    private readonly Product _secondTestProduct;
    private readonly Product _thirdTestProduct;

    private readonly string _productsRoute;
    private readonly string _firstProductRoute;

    public ProductsControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _firstTestCategory = CategoriesTestData.FirstTestCategory();
        _secondTestCategory = CategoriesTestData.SecondTestCategory();
        _firstTestProduct = ProductsTestData.FirstTestProduct(_firstTestCategory);
        _secondTestProduct = ProductsTestData.SecondTestProduct(_secondTestCategory);
        _thirdTestProduct = ProductsTestData.ThirdTestProduct(_firstTestCategory);

        _productsRoute = "products";
        _firstProductRoute = $"products/{_firstTestProduct.Id}";
    }

    [Fact]
    public async Task ShouldCreateProduct()
    {
        // Arrange
        var request = new CreateProductDto(
            _thirdTestProduct.Title,
            _thirdTestProduct.Description,
            [_firstTestCategory.Id.Value, _secondTestCategory.Id.Value],
            ProductSpecsDto.FromDomainModel(_thirdTestProduct.Specs));

        // Act
        var response = await Client.PostAsJsonAsync(_productsRoute, request, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var responseDto = await response.Content.ReadFromJsonAsync<ProductDetailsDto>(JsonOptions);
        responseDto.Should().NotBeNull();
        responseDto!.Title.Should().Be(request.Title);
        responseDto.Categories.Select(x => x.Id).Should()
            .BeEquivalentTo([_firstTestCategory.Id.Value, _secondTestCategory.Id.Value]);
        responseDto.Images.Should().BeEmpty();

        var productId = new ProductId(responseDto.Id);

        var dbProduct = await Context.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == productId);
        dbProduct.Should().NotBeNull();
        dbProduct!.Title.Should().Be(request.Title);
        dbProduct.Description.Should().Be(request.Description);
        dbProduct.Specs.Should().Be(_thirdTestProduct.Specs);
        dbProduct.UpdatedAt.Should().BeNull();

        var dbCategoryIds = await LinkedCategoryIdsAsync(productId);
        dbCategoryIds.Should().BeEquivalentTo([_firstTestCategory.Id, _secondTestCategory.Id]);

        NotificationService.WasSentFor(productId).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotCreateProductBecauseCategoryNotFound()
    {
        // Arrange
        var missingCategoryId = CategoryId.New();
        var request = new CreateProductDto(
            _thirdTestProduct.Title,
            _thirdTestProduct.Description,
            [_firstTestCategory.Id.Value, missingCategoryId.Value],
            null);

        // Act
        var response = await Client.PostAsJsonAsync(_productsRoute, request, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();

        var missingIds = ((JsonElement)problem!.Extensions["missingCategoryIds"]!)
            .EnumerateArray()
            .Select(x => x.GetGuid())
            .ToList();
        missingIds.Should().Equal(missingCategoryId.Value);

        var dbProduct = await Context.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Title == request.Title);
        dbProduct.Should().BeNull();

        NotificationService.SentProductIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotCreateProductBecauseTitleAlreadyExists()
    {
        // Arrange
        var request = new CreateProductDto(
            _firstTestProduct.Title,
            _thirdTestProduct.Description,
            [_firstTestCategory.Id.Value],
            null);

        // Act
        var response = await Client.PostAsJsonAsync(_productsRoute, request, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var dbProducts = await Context.Products.AsNoTracking()
            .Where(x => x.Title == _firstTestProduct.Title)
            .ToListAsync();
        dbProducts.Should().ContainSingle();

        NotificationService.SentProductIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotCreateProductBecauseDataIsInvalid()
    {
        // Arrange
        var request = new CreateProductDto("ab", string.Empty, [], null);

        // Act
        var response = await Client.PostAsJsonAsync(_productsRoute, request, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKeys("Title", "Description", "CategoryIds");

        var dbProducts = await Context.Products.AsNoTracking().ToListAsync();
        dbProducts.Should().ContainSingle();

        NotificationService.SentProductIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldUpdateProduct()
    {
        // Arrange
        var request = new UpdateProductDto(
            _firstTestProduct.Id.Value,
            _thirdTestProduct.Title,
            _thirdTestProduct.Description,
            [_secondTestCategory.Id.Value],
            ProductSpecsDto.FromDomainModel(_thirdTestProduct.Specs));

        // Act
        var response = await Client.PutAsJsonAsync(_productsRoute, request, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseDto = await response.Content.ReadFromJsonAsync<ProductDetailsDto>(JsonOptions);
        responseDto.Should().NotBeNull();
        responseDto!.Id.Should().Be(_firstTestProduct.Id.Value);
        responseDto.UpdatedAt.Should().NotBeNull();

        var dbProduct = await Context.Products.AsNoTracking()
            .FirstAsync(x => x.Id == _firstTestProduct.Id);
        dbProduct.Title.Should().Be(request.Title);
        dbProduct.Description.Should().Be(request.Description);
        dbProduct.Specs.Should().Be(_thirdTestProduct.Specs);
        dbProduct.UpdatedAt.Should().NotBeNull();

        var dbCategoryIds = await LinkedCategoryIdsAsync(_firstTestProduct.Id);
        dbCategoryIds.Should().Equal(_secondTestCategory.Id);
    }

    [Fact]
    public async Task ShouldNotUpdateProductBecauseCategoryNotFound()
    {
        // Arrange
        var request = new UpdateProductDto(
            _firstTestProduct.Id.Value,
            _thirdTestProduct.Title,
            _thirdTestProduct.Description,
            [_secondTestCategory.Id.Value, CategoryId.New().Value],
            null);

        // Act
        var response = await Client.PutAsJsonAsync(_productsRoute, request, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var dbProduct = await Context.Products.AsNoTracking()
            .FirstAsync(x => x.Id == _firstTestProduct.Id);
        dbProduct.Title.Should().Be(_firstTestProduct.Title);
        dbProduct.UpdatedAt.Should().BeNull();

        var dbCategoryIds = await LinkedCategoryIdsAsync(_firstTestProduct.Id);
        dbCategoryIds.Should().Equal(_firstTestCategory.Id);
    }

    [Fact]
    public async Task ShouldNotUpdateProductBecauseTitleBelongsToAnotherProduct()
    {
        // Arrange
        await Context.Products.AddAsync(_secondTestProduct);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var request = new UpdateProductDto(
            _firstTestProduct.Id.Value,
            _secondTestProduct.Title,
            _firstTestProduct.Description,
            [_firstTestCategory.Id.Value],
            null);

        // Act
        var response = await Client.PutAsJsonAsync(_productsRoute, request, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var dbProduct = await Context.Products.AsNoTracking()
            .FirstAsync(x => x.Id == _firstTestProduct.Id);
        dbProduct.Title.Should().Be(_firstTestProduct.Title);
    }

    [Fact]
    public async Task ShouldNotUpdateProductBecauseProductNotFound()
    {
        // Arrange
        var request = new UpdateProductDto(
            ProductId.New().Value,
            _thirdTestProduct.Title,
            _thirdTestProduct.Description,
            [_firstTestCategory.Id.Value],
            null);

        // Act
        var response = await Client.PutAsJsonAsync(_productsRoute, request, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldGetAllProducts()
    {
        // Arrange
        await Task.Delay(25);
        var newerProduct = ProductsTestData.SecondTestProduct(_secondTestCategory);
        await Context.Products.AddAsync(newerProduct);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        // Act
        var response = await Client.GetAsync(_productsRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var responseDtos = await response.Content.ReadFromJsonAsync<List<ProductListItemDto>>(JsonOptions);
        responseDtos.Should().NotBeNull();
        responseDtos!.Select(x => x.Id).Should().Equal(newerProduct.Id.Value, _firstTestProduct.Id.Value);
        responseDtos[0].Categories.Should().ContainSingle()
            .Which.Id.Should().Be(_secondTestCategory.Id.Value);
        responseDtos[1].Categories.Should().ContainSingle()
            .Which.Id.Should().Be(_firstTestCategory.Id.Value);
    }

    [Fact]
    public async Task ShouldGetEmptyListWhenProductsNotExist()
    {
        // Arrange
        Context.Products.RemoveRange(Context.Products);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        // Act
        var response = await Client.GetAsync(_productsRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var responseDtos = await response.Content.ReadFromJsonAsync<List<ProductListItemDto>>(JsonOptions);
        responseDtos.Should().NotBeNull();
        responseDtos.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldGetProductById()
    {
        // Act
        var response = await Client.GetAsync(_firstProductRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var responseDto = await response.Content.ReadFromJsonAsync<ProductDetailsDto>(JsonOptions);
        responseDto.Should().NotBeNull();
        responseDto!.Id.Should().Be(_firstTestProduct.Id.Value);
        responseDto.Title.Should().Be(_firstTestProduct.Title);
        responseDto.Description.Should().Be(_firstTestProduct.Description);
        responseDto.Specs.Should().Be(ProductSpecsDto.FromDomainModel(_firstTestProduct.Specs));
        responseDto.Categories.Should().ContainSingle()
            .Which.Name.Should().Be(_firstTestCategory.Name);
        responseDto.Images.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotGetProductBecauseProductNotFound()
    {
        // Act
        var response = await Client.GetAsync($"products/{ProductId.New()}");

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldDeleteProduct()
    {
        // Arrange
        await Context.ProductImages.AddRangeAsync(
            ProductImagesTestData.FirstTestImage(_firstTestProduct),
            ProductImagesTestData.SecondTestImage(_firstTestProduct));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        // Act
        var response = await Client.DeleteAsync(_firstProductRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var dbProduct = await Context.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == _firstTestProduct.Id);
        dbProduct.Should().BeNull();

        var dbLinks = await Context.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == _firstTestProduct.Id)
            .ToListAsync();
        dbLinks.Should().BeEmpty();

        var dbImages = await Context.ProductImages.AsNoTracking()
            .Where(x => x.ProductId == _firstTestProduct.Id)
            .ToListAsync();
        dbImages.Should().BeEmpty();

        var dbCategories = await Context.Categories.AsNoTracking().ToListAsync();
        dbCategories.Should().HaveCount(2);
    }

    [Fact]
    public async Task ShouldNotDeleteProductBecauseProductNotFound()
    {
        // Act
        var response = await Client.DeleteAsync($"products/{ProductId.New()}");

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var dbProduct = await Context.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == _firstTestProduct.Id);
        dbProduct.Should().NotBeNull();
    }

    private Task<List<CategoryId>> LinkedCategoryIdsAsync(ProductId productId)
        => Context.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.CategoryId)
            .ToListAsync();

    public async Task InitializeAsync()
    {
        await Context.Categories.AddRangeAsync(_firstTestCategory, _secondTestCategory);
        await Context.Products.AddAsync(_firstTestProduct);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        NotificationService.Clear();
    }

    public async Task DisposeAsync()
    {
        Context.Products.RemoveRange(Context.Products);
        await Context.SaveChangesAsync();
        Context.Categories.RemoveRange(Context.Categories);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        NotificationService.Clear();
    }
}
