using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Categories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;

namespace Api.Tests.Integration.Categories;

// PUT /categories (розширення до розд. 5 ТЗ): перейменування, збереження власної
// назви, конфлікт із чужою назвою, невідомий id і валідація.
public sealed class UpdateCategoryTests(IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task Update_WithNewName_ReturnsOkAndPersistsName()
    {
        var category = await SeedCategoryAsync();
        var newName = CategoriesData.UniqueName("Bass guitars");

        var response = await Client.PutAsJsonAsync("/categories", new { id = category.Id.Value, name = newName });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CategoryDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(category.Id.Value);
        body.Name.Should().Be(newName);
        body.CreatedAt.Should().BeCloseTo(category.CreatedAt, TimeSpan.FromSeconds(1));

        var persisted = await FindAsync(category.Id);
        persisted.Name.Should().Be(newName);
    }

    [Fact]
    public async Task Update_WithSameName_ReturnsOk()
    {
        var category = await SeedCategoryAsync();

        var response = await Client.PutAsJsonAsync("/categories", new { id = category.Id.Value, name = category.Name });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_WithNameOfAnotherCategory_ReturnsConflictAndKeepsName()
    {
        var category = await SeedCategoryAsync();
        var other = await SeedCategoryAsync();

        var response = await Client.PutAsJsonAsync("/categories", new { id = category.Id.Value, name = other.Name });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status409Conflict);
        problem.Title.Should().Contain(other.Name);

        var persisted = await FindAsync(category.Id);
        persisted.Name.Should().Be(category.Name);
    }

    [Fact]
    public async Task Update_WithUnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync(
            "/categories",
            new { id = Guid.NewGuid(), name = CategoriesData.UniqueName("Nobody") });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Update_WithInvalidData_ReturnsValidationProblem()
    {
        var response = await Client.PutAsJsonAsync("/categories", new { id = Guid.Empty, name = "ab" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKeys("CategoryId", "Name");
    }

    private async Task<Category> SeedCategoryAsync()
    {
        var category = CategoriesData.MainCategory();
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        return category;
    }

    private Task<Category> FindAsync(CategoryId id)
        => DbContext.Categories
            .AsNoTracking()
            .SingleAsync(category => category.Id == id);
}
