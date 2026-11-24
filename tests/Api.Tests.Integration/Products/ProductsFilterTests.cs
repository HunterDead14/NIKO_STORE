using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Categories;
using Domain.Products;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;
using Tests.Data.Products;
using Xunit;

namespace Api.Tests.Integration.Products;

public class ProductsFilterTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Category _firstTestCategory;
    private readonly Category _secondTestCategory;

    private Product _firstTestProduct = null!;
    private Product _secondTestProduct = null!;
    private Product _thirdTestProduct = null!;
    private Product _fourthTestProduct = null!;

    private readonly string _filterRoute;

    public ProductsFilterTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _firstTestCategory = CategoriesTestData.FirstTestCategory();
        _secondTestCategory = CategoriesTestData.SecondTestCategory();

        _filterRoute = "products/filter";
    }

    [Fact]
    public async Task ShouldFilterByTitle()
    {
        var ids = await FilterAsync(EmptyFilter() with { Title = "les paul" });

        ids.Should().Equal(_secondTestProduct.Id.Value);
    }

    [Fact]
    public async Task ShouldFilterByDescription()
    {
        var ids = await FilterAsync(EmptyFilter() with { Description = "electric" });

        ids.Should().BeEquivalentTo([_firstTestProduct.Id.Value, _secondTestProduct.Id.Value]);
    }

    [Fact]
    public async Task ShouldFilterByBrand()
    {
        var ids = await FilterAsync(EmptyFilter() with { Brand = "YAMAHA" });

        ids.Should().Equal(_thirdTestProduct.Id.Value);
    }

    [Fact]
    public async Task ShouldFilterByModel()
    {
        var ids = await FilterAsync(EmptyFilter() with { Model = "sr5" });

        ids.Should().Equal(_fourthTestProduct.Id.Value);
    }

    [Fact]
    public async Task ShouldFilterByColor()
    {
        var ids = await FilterAsync(EmptyFilter() with { Color = "cherry" });

        ids.Should().Equal(_secondTestProduct.Id.Value);
    }

    [Fact]
    public async Task ShouldFilterByCondition()
    {
        var ids = await FilterAsync(EmptyFilter() with { Condition = "used" });

        ids.Should().BeEquivalentTo([_secondTestProduct.Id.Value, _fourthTestProduct.Id.Value]);
    }

    [Fact]
    public async Task ShouldFilterByManufactureYearFrom()
    {
        var ids = await FilterAsync(EmptyFilter() with { ManufactureYearFrom = 2020 });

        ids.Should().BeEquivalentTo([_firstTestProduct.Id.Value, _thirdTestProduct.Id.Value]);
    }

    [Fact]
    public async Task ShouldFilterByManufactureYearTo()
    {
        var ids = await FilterAsync(EmptyFilter() with { ManufactureYearTo = 2019 });

        ids.Should().BeEquivalentTo([_secondTestProduct.Id.Value, _fourthTestProduct.Id.Value]);
    }

    [Fact]
    public async Task ShouldIncludeManufactureYearRangeBoundaries()
    {
        var ids = await FilterAsync(EmptyFilter() with { ManufactureYearFrom = 2019, ManufactureYearTo = 2020 });

        ids.Should().BeEquivalentTo([_firstTestProduct.Id.Value, _fourthTestProduct.Id.Value]);
    }

    [Fact]
    public async Task ShouldFilterByCreatedAtFrom()
    {
        var createdAt = await PersistedCreatedAtAsync(_thirdTestProduct.Id);

        var ids = await FilterAsync(EmptyFilter() with { CreatedAtFrom = createdAt });

        ids.Should().Equal(_fourthTestProduct.Id.Value, _thirdTestProduct.Id.Value);
    }

    [Fact]
    public async Task ShouldFilterByCreatedAtTo()
    {
        var createdAt = await PersistedCreatedAtAsync(_secondTestProduct.Id);

        var ids = await FilterAsync(EmptyFilter() with { CreatedAtTo = createdAt });

        ids.Should().Equal(_secondTestProduct.Id.Value, _firstTestProduct.Id.Value);
    }

    [Fact]
    public async Task ShouldFilterByCreatedAtRangeWithBothBoundaries()
    {
        var from = await PersistedCreatedAtAsync(_secondTestProduct.Id);
        var to = await PersistedCreatedAtAsync(_thirdTestProduct.Id);

        var ids = await FilterAsync(EmptyFilter() with { CreatedAtFrom = from, CreatedAtTo = to });

        ids.Should().Equal(_thirdTestProduct.Id.Value, _secondTestProduct.Id.Value);
    }

    [Fact]
    public async Task ShouldFilterByCombinationOfParameters()
    {
        var ids = await FilterAsync(EmptyFilter() with { Condition = "new", ManufactureYearFrom = 2021 });

        ids.Should().Equal(_thirdTestProduct.Id.Value);
    }

    [Fact]
    public async Task ShouldIncludeCategoriesInFilteredProducts()
    {
        var response = await Client.PostAsJsonAsync(_filterRoute, EmptyFilter() with { Brand = "Gibson" }, JsonOptions);

        response.IsSuccessStatusCode.Should().BeTrue();

        var responseDtos = await response.Content.ReadFromJsonAsync<List<ProductListItemDto>>(JsonOptions);
        responseDtos.Should().NotBeNull();
        responseDtos!.Should().ContainSingle()
            .Which.Categories.Should().ContainSingle()
            .Which.Name.Should().Be(_secondTestCategory.Name);
    }

    [Fact]
    public async Task ShouldReturnAllProductsWhenFilterIsEmpty()
    {
        var ids = await FilterAsync(EmptyFilter());

        ids.Should().Equal(
            _fourthTestProduct.Id.Value,
            _thirdTestProduct.Id.Value,
            _secondTestProduct.Id.Value,
            _firstTestProduct.Id.Value);
    }

    [Fact]
    public async Task ShouldReturnEmptyListWhenNothingMatches()
    {
        var ids = await FilterAsync(EmptyFilter() with { Brand = "Fender", Color = "Black" });

        ids.Should().BeEmpty();
    }

    private static ProductFilterDto EmptyFilter()
        => new(null, null, null, null, null, null, null, null, null, null);

    private async Task<List<Guid>> FilterAsync(ProductFilterDto filter)
    {
        var response = await Client.PostAsJsonAsync(_filterRoute, filter, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseDtos = await response.Content.ReadFromJsonAsync<List<ProductListItemDto>>(JsonOptions);
        responseDtos.Should().NotBeNull();

        return responseDtos!.Select(x => x.Id).ToList();
    }

    private Task<DateTime> PersistedCreatedAtAsync(ProductId productId)
        => Context.Products.AsNoTracking()
            .Where(x => x.Id == productId)
            .Select(x => x.CreatedAt)
            .SingleAsync();

    public async Task InitializeAsync()
    {
        await Context.Categories.AddRangeAsync(_firstTestCategory, _secondTestCategory);

        _firstTestProduct = ProductsTestData.FirstTestProduct(_firstTestCategory);
        await Task.Delay(10);
        _secondTestProduct = ProductsTestData.SecondTestProduct(_secondTestCategory);
        await Task.Delay(10);
        _thirdTestProduct = ProductsTestData.ThirdTestProduct(_firstTestCategory);
        await Task.Delay(10);
        _fourthTestProduct = ProductsTestData.FourthTestProduct(_firstTestCategory, _secondTestCategory);

        await Context.Products.AddRangeAsync(_firstTestProduct, _secondTestProduct, _thirdTestProduct, _fourthTestProduct);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    public async Task DisposeAsync()
    {
        Context.Products.RemoveRange(Context.Products);
        await Context.SaveChangesAsync();
        Context.Categories.RemoveRange(Context.Categories);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }
}
