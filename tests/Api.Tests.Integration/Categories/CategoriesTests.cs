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

public sealed class CategoriesTests(IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task Create_WithValidName_ReturnsCreatedAndPersistsCategory()
    {
        var name = CategoriesData.UniqueName("Acoustic guitars");

        var response = await Client.PostAsJsonAsync("/categories", new { name });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<CategoryDto>();
        created.Should().NotBeNull();
        created!.Name.Should().Be(name);

        response.Headers.Location!.ToString().Should().Contain(created.Id.ToString());

        var persisted = await DbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.Id == new CategoryId(created.Id));

        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be(name);
    }

    [Fact]
    public async Task Create_WithDuplicateName_ReturnsConflict()
    {
        var category = CategoriesData.MainCategory();
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync("/categories", new { name = category.Name });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status409Conflict);
        problem.Title.Should().Contain(category.Name);

        var count = await DbContext.Categories
            .AsNoTracking()
            .CountAsync(item => item.Name == category.Name);

        count.Should().Be(1);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsValidationProblem()
    {
        var response = await Client.PostAsJsonAsync("/categories", new { name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey("Name");

        var persisted = await DbContext.Categories
            .AsNoTracking()
            .AnyAsync(category => category.Name == "");

        persisted.Should().BeFalse();
    }

    [Fact]
    public async Task GetById_WithExistingCategory_ReturnsCategory()
    {
        var category = CategoriesData.MainCategory();
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync($"/categories/{category.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CategoryDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(category.Id.Value);
        body.Name.Should().Be(category.Name);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        var unknownId = Guid.NewGuid();

        var response = await Client.GetAsync($"/categories/{unknownId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetAll_ReturnsCategoriesSortedByName()
    {
        var firstName = CategoriesData.UniqueName("Ordering A");
        var secondName = CategoriesData.UniqueName("Ordering B");

        DbContext.Categories.AddRange(
            CategoriesData.WithName(firstName),
            CategoriesData.WithName(secondName));
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync("/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        categories.Should().NotBeNull();

        var firstIndex = categories!.FindIndex(category => category.Name == firstName);
        var secondIndex = categories.FindIndex(category => category.Name == secondName);

        firstIndex.Should().BeGreaterThanOrEqualTo(0);
        secondIndex.Should().BeGreaterThanOrEqualTo(0);
        firstIndex.Should().BeLessThan(secondIndex);
    }
}
