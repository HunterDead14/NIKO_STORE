using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

namespace Api.Tests.Integration.ProductImages;

public sealed class ProductImagesTests(IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<IntegrationTestFixture>
{
    private const int TooLargeSize = 5 * 1024 * 1024 + 1;

    [Fact]
    public async Task Upload_WithValidJpeg_ReturnsCreatedAndPersistsRecordAndFile()
    {
        var product = await SeedProductAsync();
        var content = JpegBytes();

        using var form = CreateForm(content, "guitar.jpg", "image/jpeg");

        var response = await Client.PostAsync($"/products/{product.Id.Value}/images", form);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var image = await response.Content.ReadFromJsonAsync<ProductImageDto>();
        image.Should().NotBeNull();
        image!.OriginalName.Should().Be("guitar.jpg");
        image.ContentType.Should().Be("image/jpeg");
        image.SizeBytes.Should().Be(content.Length);
        image.SortOrder.Should().Be(0);
        image.ImageUrl.Should().Be($"/products/{product.Id.Value}/images/{image.Id}");
        response.Headers.Location!.ToString().Should().Contain(image.Id.ToString());

        var persisted = await DbContext.ProductImages
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == new ProductImageId(image.Id));

        persisted.Should().NotBeNull();
        persisted!.ProductId.Should().Be(product.Id);
        persisted.OriginalName.Should().Be("guitar.jpg");
        persisted.StoredExtension.Should().Be(".jpg");

        File.Exists(ImageFilePath(product.Id.Value, image.Id, ".jpg")).Should().BeTrue();
    }

    [Fact]
    public async Task Upload_ToUnknownProduct_ReturnsNotFound()
    {
        var productId = Guid.NewGuid();

        using var form = CreateForm(JpegBytes(), "guitar.jpg", "image/jpeg");

        var response = await Client.PostAsync($"/products/{productId}/images", form);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);

        var count = await DbContext.ProductImages
            .AsNoTracking()
            .CountAsync(image => image.ProductId == new ProductId(productId));

        count.Should().Be(0);
    }

    [Fact]
    public async Task Upload_WithFakePng_ReturnsBadRequest()
    {
        var product = await SeedProductAsync();
        var content = "this is definitely not a png"u8.ToArray();

        using var form = CreateForm(content, "fake.png", "image/png");

        var response = await Client.PostAsync($"/products/{product.Id.Value}/images", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Contain("only JPEG, PNG and WebP");

        var count = await DbContext.ProductImages
            .AsNoTracking()
            .CountAsync(image => image.ProductId == product.Id);

        count.Should().Be(0);
    }

    [Fact]
    public async Task Upload_WithTooLargeFile_ReturnsBadRequest()
    {
        var product = await SeedProductAsync();

        var content = new byte[TooLargeSize];
        content[0] = 0xFF;
        content[1] = 0xD8;
        content[2] = 0xFF;

        using var form = CreateForm(content, "huge.jpg", "image/jpeg");

        var response = await Client.PostAsync($"/products/{product.Id.Value}/images", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Contain("5 MB");

        var count = await DbContext.ProductImages
            .AsNoTracking()
            .CountAsync(image => image.ProductId == product.Id);

        count.Should().Be(0);
    }

    [Fact]
    public async Task Download_ReturnsOriginalBytesContentTypeAndCacheHeaders()
    {
        var product = await SeedProductAsync();
        var content = JpegBytes();
        var image = await UploadAsync(product.Id.Value, content, "guitar.jpg", "image/jpeg");

        var response = await Client.GetAsync(image.ImageUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.ToString().Should().Contain("immutable");
        response.Headers.ETag.Should().NotBeNull();

        var downloaded = await response.Content.ReadAsByteArrayAsync();
        downloaded.Should().Equal(content);
    }

    [Fact]
    public async Task GetAll_ReturnsImagesSortedBySortOrder()
    {
        var product = await SeedProductAsync();

        var first = await UploadAsync(product.Id.Value, JpegBytes(), "first.jpg", "image/jpeg");
        var second = await UploadAsync(product.Id.Value, PngBytes(), "second.png", "image/png");

        var response = await Client.GetAsync($"/products/{product.Id.Value}/images");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var images = await response.Content.ReadFromJsonAsync<List<ProductImageDto>>();
        images.Should().NotBeNull();
        images.Should().HaveCount(2);
        images![0].Id.Should().Be(first.Id);
        images[0].SortOrder.Should().Be(0);
        images[1].Id.Should().Be(second.Id);
        images[1].SortOrder.Should().Be(1);
        images[1].ContentType.Should().Be("image/png");
    }

    [Fact]
    public async Task Download_WithForeignImageId_ReturnsNotFound()
    {
        var owner = await SeedProductAsync();
        var image = await UploadAsync(owner.Id.Value, JpegBytes(), "guitar.jpg", "image/jpeg");

        var anotherProduct = await SeedProductAsync();

        var response = await Client.GetAsync($"/products/{anotherProduct.Id.Value}/images/{image.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Delete_RemovesRecordAndFile()
    {
        var product = await SeedProductAsync();
        var image = await UploadAsync(product.Id.Value, JpegBytes(), "guitar.jpg", "image/jpeg");

        var response = await Client.DeleteAsync($"/products/{product.Id.Value}/images/{image.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var exists = await DbContext.ProductImages
            .AsNoTracking()
            .AnyAsync(item => item.Id == new ProductImageId(image.Id));

        exists.Should().BeFalse();
        File.Exists(ImageFilePath(product.Id.Value, image.Id, ".jpg")).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WithForeignImageId_ReturnsNotFoundAndKeepsImage()
    {
        var owner = await SeedProductAsync();
        var image = await UploadAsync(owner.Id.Value, JpegBytes(), "guitar.jpg", "image/jpeg");

        var anotherProduct = await SeedProductAsync();

        var response = await Client.DeleteAsync($"/products/{anotherProduct.Id.Value}/images/{image.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var exists = await DbContext.ProductImages
            .AsNoTracking()
            .AnyAsync(item => item.Id == new ProductImageId(image.Id));

        exists.Should().BeTrue();
        File.Exists(ImageFilePath(owner.Id.Value, image.Id, ".jpg")).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProduct_RemovesImageFolder()
    {
        var product = await SeedProductAsync();
        await UploadAsync(product.Id.Value, JpegBytes(), "guitar.jpg", "image/jpeg");

        var folder = ProductFolder(product.Id.Value);
        Directory.Exists(folder).Should().BeTrue();

        var response = await Client.DeleteAsync($"/products/{product.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Directory.Exists(folder).Should().BeFalse();
    }

    private async Task<Product> SeedProductAsync()
    {
        var category = CategoriesData.MainCategory();
        DbContext.Categories.Add(category);

        var product = ProductsData.MainProduct(category);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        return product;
    }

    private async Task<ProductImageDto> UploadAsync(
        Guid productId,
        byte[] content,
        string fileName,
        string contentType)
    {
        using var form = CreateForm(content, fileName, contentType);

        var response = await Client.PostAsync($"/products/{productId}/images", form);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var image = await response.Content.ReadFromJsonAsync<ProductImageDto>();
        image.Should().NotBeNull();

        return image!;
    }

    private static MultipartFormDataContent CreateForm(byte[] content, string fileName, string contentType)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private static byte[] JpegBytes()
        => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];

    private static byte[] PngBytes()
        => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private string ProductFolder(Guid productId)
        => Path.Combine(Fixture.StorageRoot, "products", productId.ToString());

    private string ImageFilePath(Guid productId, Guid imageId, string extension)
        => Path.Combine(ProductFolder(productId), $"{imageId}{extension}");
}
