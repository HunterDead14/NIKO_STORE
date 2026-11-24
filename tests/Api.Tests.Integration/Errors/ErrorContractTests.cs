using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Tests.Common;
using Xunit;

namespace Api.Tests.Integration.Errors;

public class ErrorContractTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ShouldReturnProblemDetailsForUnknownRoute()
    {
        var response = await Client.GetAsync("this-route-does-not-exist");

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.NotFound);
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public async Task ShouldReturnProblemDetailsForUnsupportedMethod()
    {
        var response = await Client.PatchAsync("categories", content: null);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task ShouldReturnValidationProblemDetailsForMalformedJson()
    {
        using var content = new StringContent("{ \"name\": ", Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("categories", content);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
    }
}
