using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Categories;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;
using Xunit;

namespace Api.Tests.Integration.Categories;

public class CategoriesControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Category _firstTestCategory;
    private readonly Category _secondTestCategory;
    private readonly Category _thirdTestCategory;

    private readonly string _categoriesRoute;
    private readonly string _firstCategoryRoute;

    public CategoriesControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _firstTestCategory = CategoriesTestData.FirstTestCategory();
        _secondTestCategory = CategoriesTestData.SecondTestCategory();
        _thirdTestCategory = CategoriesTestData.ThirdTestCategory();

        _categoriesRoute = "categories";
        _firstCategoryRoute = $"categories/{_firstTestCategory.Id}";
    }

    [Fact]
    public async Task ShouldCreateCategory()
    {
        var request = new CreateCategoryDto(_thirdTestCategory.Name);

        var response = await Client.PostAsJsonAsync(_categoriesRoute, request, JsonOptions);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var responseDto = await response.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
        responseDto.Should().NotBeNull();
        responseDto!.Name.Should().Be(request.Name);
        response.Headers.Location!.ToString().Should().Contain(responseDto.Id.ToString());

        var dbCategory = await Context.Categories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == new CategoryId(responseDto.Id));
        dbCategory.Should().NotBeNull();
        dbCategory!.Name.Should().Be(request.Name);
    }

    [Fact]
    public async Task ShouldNotCreateCategoryBecauseNameAlreadyExists()
    {
        var request = new CreateCategoryDto(_firstTestCategory.Name);

        var response = await Client.PostAsJsonAsync(_categoriesRoute, request, JsonOptions);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Title.Should().Contain(_firstTestCategory.Name);

        var dbCategories = await Context.Categories.AsNoTracking()
            .Where(x => x.Name == _firstTestCategory.Name)
            .ToListAsync();
        dbCategories.Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ShouldNotCreateCategoryBecauseNameIsEmpty(string name)
    {
        var request = new CreateCategoryDto(name);

        var response = await Client.PostAsJsonAsync(_categoriesRoute, request, JsonOptions);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey("Name");

        var dbCategories = await Context.Categories.AsNoTracking().ToListAsync();
        dbCategories.Should().HaveCount(2);
    }

    [Fact]
    public async Task ShouldNotCreateCategoryBecauseNameIsTooLong()
    {
        var request = new CreateCategoryDto(new string('a', 256));

        var response = await Client.PostAsJsonAsync(_categoriesRoute, request, JsonOptions);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task ShouldUpdateCategory()
    {
        var request = new UpdateCategoryDto(_firstTestCategory.Id.Value, _thirdTestCategory.Name);

        var response = await Client.PutAsJsonAsync(_categoriesRoute, request, JsonOptions);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseDto = await response.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
        responseDto.Should().NotBeNull();
        responseDto!.Id.Should().Be(_firstTestCategory.Id.Value);
        responseDto.Name.Should().Be(request.Name);

        var dbCategory = await Context.Categories.AsNoTracking()
            .FirstAsync(x => x.Id == _firstTestCategory.Id);
        dbCategory.Name.Should().Be(request.Name);
        dbCategory.CreatedAt.Should().BeCloseTo(_firstTestCategory.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ShouldUpdateCategoryWithSameName()
    {
        var request = new UpdateCategoryDto(_firstTestCategory.Id.Value, _firstTestCategory.Name);

        var response = await Client.PutAsJsonAsync(_categoriesRoute, request, JsonOptions);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldNotUpdateCategoryBecauseNameBelongsToAnotherCategory()
    {
        var request = new UpdateCategoryDto(_firstTestCategory.Id.Value, _secondTestCategory.Name);

        var response = await Client.PutAsJsonAsync(_categoriesRoute, request, JsonOptions);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var dbCategory = await Context.Categories.AsNoTracking()
            .FirstAsync(x => x.Id == _firstTestCategory.Id);
        dbCategory.Name.Should().Be(_firstTestCategory.Name);
    }

    [Fact]
    public async Task ShouldNotUpdateCategoryBecauseCategoryNotFound()
    {
        var request = new UpdateCategoryDto(CategoryId.New().Value, _thirdTestCategory.Name);

        var response = await Client.PutAsJsonAsync(_categoriesRoute, request, JsonOptions);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotUpdateCategoryBecauseDataIsInvalid()
    {
        var request = new UpdateCategoryDto(Guid.Empty, string.Empty);

        var response = await Client.PutAsJsonAsync(_categoriesRoute, request, JsonOptions);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKeys("CategoryId", "Name");
    }

    [Fact]
    public async Task ShouldGetAllCategories()
    {
        var response = await Client.GetAsync(_categoriesRoute);

        response.IsSuccessStatusCode.Should().BeTrue();

        var responseDtos = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);
        responseDtos.Should().NotBeNull();
        responseDtos!.Select(x => x.Id).Should()
            .BeEquivalentTo([_firstTestCategory.Id.Value, _secondTestCategory.Id.Value]);
        responseDtos.Select(x => x.Name).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task ShouldGetEmptyListWhenCategoriesNotExist()
    {
        Context.Categories.RemoveRange(Context.Categories);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var response = await Client.GetAsync(_categoriesRoute);

        response.IsSuccessStatusCode.Should().BeTrue();

        var responseDtos = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);
        responseDtos.Should().NotBeNull();
        responseDtos.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldGetCategoryById()
    {
        var response = await Client.GetAsync(_firstCategoryRoute);

        response.IsSuccessStatusCode.Should().BeTrue();

        var responseDto = await response.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
        responseDto.Should().NotBeNull();
        responseDto!.Id.Should().Be(_firstTestCategory.Id.Value);
        responseDto.Name.Should().Be(_firstTestCategory.Name);
        responseDto.CreatedAt.Should().BeCloseTo(_firstTestCategory.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ShouldNotGetCategoryBecauseCategoryNotFound()
    {
        var response = await Client.GetAsync($"categories/{CategoryId.New()}");

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public async Task InitializeAsync()
    {
        await Context.Categories.AddRangeAsync(_firstTestCategory, _secondTestCategory);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    public async Task DisposeAsync()
    {
        Context.Categories.RemoveRange(Context.Categories);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }
}
