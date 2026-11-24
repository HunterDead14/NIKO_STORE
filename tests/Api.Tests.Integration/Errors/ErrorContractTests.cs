using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tests.Common;

namespace Api.Tests.Integration.Errors;

public sealed class ErrorContractTests(IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task UnknownRoute_ReturnsNotFoundProblemDetails()
    {
        var response = await Client.GetAsync("/this-route-does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public async Task UnsupportedMethod_ReturnsMethodNotAllowedProblemDetails()
    {
        var response = await Client.PatchAsync("/categories", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status405MethodNotAllowed);
    }

    [Fact]
    public async Task MalformedJson_ReturnsValidationProblemDetails()
    {
        using var content = new StringContent("{ \"name\": ", System.Text.Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/categories", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status400BadRequest);
    }
}
