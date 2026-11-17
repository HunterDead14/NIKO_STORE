using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using FluentAssertions;
using Tests.Common;

namespace Api.Tests.Integration;

public sealed class ApiSmokeTests(IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetCategories_OnEmptyDatabase_ReturnsEmptyArray()
    {
        var response = await Client.GetAsync("/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();

        categories.Should().NotBeNull();
        categories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProducts_OnEmptyDatabase_ReturnsEmptyArray()
    {
        var response = await Client.GetAsync("/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var products = await response.Content.ReadFromJsonAsync<List<ProductListItemDto>>();

        products.Should().NotBeNull();
        products.Should().BeEmpty();
    }
}
