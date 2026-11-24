using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Categories;
using Domain.Products;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;
using Tests.Data.Products;
using Xunit;

namespace Api.Tests.Integration.ProductImages;

public class ProductImagesControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private const int TooLargeSize = 5 * 1024 * 1024 + 1;

    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private readonly Category _firstTestCategory;
    private readonly Product _firstTestProduct;
    private readonly Product _secondTestProduct;

    private readonly string _firstProductImagesRoute;
    private readonly string _secondProductImagesRoute;

    public ProductImagesControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _firstTestCategory = CategoriesTestData.FirstTestCategory();
        _firstTestProduct = ProductsTestData.FirstTestProduct(_firstTestCategory);
        _secondTestProduct = ProductsTestData.SecondTestProduct(_firstTestCategory);

        _firstProductImagesRoute = $"products/{_firstTestProduct.Id}/images";
        _secondProductImagesRoute = $"products/{_secondTestProduct.Id}/images";
    }

    [Fact]
    public async Task ShouldUploadImage()
    {
        using var form = CreateForm(JpegBytes, "guitar.jpg", "image/jpeg");

        var response = await Client.PostAsync(_firstProductImagesRoute, form);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var responseDto = await response.Content.ReadFromJsonAsync<ProductImageDto>(JsonOptions);
        responseDto.Should().NotBeNull();
        responseDto!.OriginalName.Should().Be("guitar.jpg");
        responseDto.ContentType.Should().Be("image/jpeg");
        responseDto.SizeBytes.Should().Be(JpegBytes.Length);
        responseDto.SortOrder.Should().Be(0);
        responseDto.ImageUrl.Should().Be($"/{_firstProductImagesRoute}/{responseDto.Id}");

        var dbImage = await Context.ProductImages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == new ProductImageId(responseDto.Id));
        dbImage.Should().NotBeNull();
        dbImage!.ProductId.Should().Be(_firstTestProduct.Id);
        dbImage.OriginalName.Should().Be("guitar.jpg");
        dbImage.StoredExtension.Should().Be(".jpg");

        File.Exists(ImageFilePath(_firstTestProduct.Id, responseDto.Id, ".jpg")).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotUploadImageBecauseProductNotFound()
    {
        var productId = ProductId.New();
        using var form = CreateForm(JpegBytes, "guitar.jpg", "image/jpeg");

        var response = await Client.PostAsync($"products/{productId}/images", form);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var dbImages = await Context.ProductImages.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync();
        dbImages.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotUploadImageBecauseFileIsNotImage()
    {
        using var form = CreateForm("this is definitely not a png"u8.ToArray(), "fake.png", "image/png");

        var response = await Client.PostAsync(_firstProductImagesRoute, form);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Title.Should().Contain("only JPEG, PNG and WebP");
        problem.Errors.Should().ContainKey("File");

        var dbImages = await Context.ProductImages.AsNoTracking()
            .Where(x => x.ProductId == _firstTestProduct.Id)
            .ToListAsync();
        dbImages.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotUploadImageBecauseFileIsTooLarge()
    {
        var content = new byte[TooLargeSize];
        JpegBytes.AsSpan(0, 3).CopyTo(content);
        using var form = CreateForm(content, "huge.jpg", "image/jpeg");

        var response = await Client.PostAsync(_firstProductImagesRoute, form);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Title.Should().Contain("5 MB");

        var dbImages = await Context.ProductImages.AsNoTracking()
            .Where(x => x.ProductId == _firstTestProduct.Id)
            .ToListAsync();
        dbImages.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldGetImages()
    {
        var firstImage = await UploadAsync(_firstProductImagesRoute, JpegBytes, "first.jpg", "image/jpeg");
        var secondImage = await UploadAsync(_firstProductImagesRoute, PngBytes, "second.png", "image/png");

        var response = await Client.GetAsync(_firstProductImagesRoute);

        response.IsSuccessStatusCode.Should().BeTrue();

        var responseDtos = await response.Content.ReadFromJsonAsync<List<ProductImageDto>>(JsonOptions);
        responseDtos.Should().NotBeNull();
        responseDtos!.Select(x => x.Id).Should().Equal(firstImage.Id, secondImage.Id);
        responseDtos.Select(x => x.SortOrder).Should().Equal(0, 1);
        responseDtos[1].ContentType.Should().Be("image/png");
    }

    [Fact]
    public async Task ShouldNotGetImagesBecauseProductNotFound()
    {
        var response = await Client.GetAsync($"products/{ProductId.New()}/images");

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldDownloadImage()
    {
        var image = await UploadAsync(_firstProductImagesRoute, JpegBytes, "guitar.jpg", "image/jpeg");

        var response = await Client.GetAsync(image.ImageUrl);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
        response.Headers.CacheControl!.ToString().Should().Contain("immutable");
        response.Headers.ETag.Should().NotBeNull();

        var content = await response.Content.ReadAsByteArrayAsync();
        content.Should().Equal(JpegBytes);
    }

    [Fact]
    public async Task ShouldNotDownloadImageBecauseImageBelongsToAnotherProduct()
    {
        var image = await UploadAsync(_firstProductImagesRoute, JpegBytes, "guitar.jpg", "image/jpeg");

        var response = await Client.GetAsync($"{_secondProductImagesRoute}/{image.Id}");

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldDeleteImage()
    {
        var image = await UploadAsync(_firstProductImagesRoute, JpegBytes, "guitar.jpg", "image/jpeg");

        var response = await Client.DeleteAsync($"{_firstProductImagesRoute}/{image.Id}");

        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var dbImage = await Context.ProductImages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == new ProductImageId(image.Id));
        dbImage.Should().BeNull();

        File.Exists(ImageFilePath(_firstTestProduct.Id, image.Id, ".jpg")).Should().BeFalse();
    }

    [Fact]
    public async Task ShouldNotDeleteImageBecauseImageBelongsToAnotherProduct()
    {
        var image = await UploadAsync(_firstProductImagesRoute, JpegBytes, "guitar.jpg", "image/jpeg");

        var response = await Client.DeleteAsync($"{_secondProductImagesRoute}/{image.Id}");

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var dbImage = await Context.ProductImages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == new ProductImageId(image.Id));
        dbImage.Should().NotBeNull();

        File.Exists(ImageFilePath(_firstTestProduct.Id, image.Id, ".jpg")).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldDeleteImagesWhenProductIsDeleted()
    {
        var image = await UploadAsync(_firstProductImagesRoute, JpegBytes, "guitar.jpg", "image/jpeg");
        Directory.Exists(ProductFolder(_firstTestProduct.Id)).Should().BeTrue();

        var response = await Client.DeleteAsync($"products/{_firstTestProduct.Id}");

        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var dbImage = await Context.ProductImages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == new ProductImageId(image.Id));
        dbImage.Should().BeNull();

        Directory.Exists(ProductFolder(_firstTestProduct.Id)).Should().BeFalse();
    }

    private async Task<ProductImageDto> UploadAsync(string route, byte[] content, string fileName, string contentType)
    {
        using var form = CreateForm(content, fileName, contentType);

        var response = await Client.PostAsync(route, form);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var image = await response.Content.ReadFromJsonAsync<ProductImageDto>(JsonOptions);
        image.Should().NotBeNull();

        return image!;
    }

    private static MultipartFormDataContent CreateForm(byte[] content, string fileName, string contentType)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private string ProductFolder(ProductId productId)
        => Path.Combine(StorageRootPath, productId.ToString());

    private string ImageFilePath(ProductId productId, Guid imageId, string extension)
        => Path.Combine(ProductFolder(productId), $"{imageId}{extension}");

    public async Task InitializeAsync()
    {
        await Context.Categories.AddAsync(_firstTestCategory);
        await Context.Products.AddRangeAsync(_firstTestProduct, _secondTestProduct);
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

        foreach (var directory in Directory.EnumerateDirectories(StorageRootPath))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
