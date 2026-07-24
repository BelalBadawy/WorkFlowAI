using Microsoft.AspNetCore.Hosting;
using WFAI.Application.Dtos.Common;
using WFAI.Infrastructure.Services;
using WFAI.Infrastructure.Tests.Fixtures;

namespace WFAI.Infrastructure.Tests.Services;

public class LocalFileStorageServiceTests : IClassFixture<TempDirectoryFixture>
{
    private readonly TempDirectoryFixture _fixture;

    public LocalFileStorageServiceTests(TempDirectoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveFileAsync_should_persist_file_under_web_root_folder()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.WebRootPath).Returns(_fixture.RootPath);

        var service = new LocalFileStorageService(environment.Object);
        await using var stream = new MemoryStream("seed-image"u8.ToArray());
        var file = new FileData
        {
            Content = stream,
            FileName = "banner.png",
            ContentType = "image/png",
            Length = stream.Length
        };

        var savedFileName = await service.SaveFileAsync(file, "images", CancellationToken.None);
        var savedPath = Path.Combine(_fixture.RootPath, "images", savedFileName);

        savedFileName.Should().EndWith(".png");
        File.Exists(savedPath).Should().BeTrue();
        (await File.ReadAllTextAsync(savedPath)).Should().Be("seed-image");
    }

    [Fact]
    public async Task SaveFileAsync_should_fall_back_to_current_directory_when_web_root_is_missing()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var fallbackRoot = Path.Combine(currentDirectory, "wwwroot", "avatars");

        try
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.WebRootPath).Returns(string.Empty);

            var service = new LocalFileStorageService(environment.Object);
            await using var stream = new MemoryStream("avatar"u8.ToArray());
            var file = new FileData
            {
                Content = stream,
                FileName = "avatar.jpg",
                ContentType = "image/jpeg",
                Length = stream.Length
            };

            var savedFileName = await service.SaveFileAsync(file, "avatars", CancellationToken.None);

            File.Exists(Path.Combine(fallbackRoot, savedFileName)).Should().BeTrue();
        }
        finally
        {
            var wwwrootPath = Path.Combine(currentDirectory, "wwwroot");

            if (Directory.Exists(wwwrootPath))
            {
                Directory.Delete(wwwrootPath, recursive: true);
            }
        }
    }

    [Fact]
    public void DeleteFile_should_remove_existing_file_and_ignore_missing_ones()
    {
        var folder = Path.Combine(_fixture.RootPath, "docs");
        Directory.CreateDirectory(folder);
        var fileName = "sample.txt";
        var fullPath = Path.Combine(folder, fileName);
        File.WriteAllText(fullPath, "content");

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.WebRootPath).Returns(_fixture.RootPath);

        var service = new LocalFileStorageService(environment.Object);

        service.DeleteFile(fileName, "docs");
        service.DeleteFile("missing.txt", "docs");

        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task SaveFileAsync_should_honor_cancellation_token_when_web_root_exists()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.WebRootPath).Returns(_fixture.RootPath);

        var service = new LocalFileStorageService(environment.Object);
        await using var stream = new MemoryStream(new byte[1024]);
        var file = new FileData
        {
            Content = stream,
            FileName = "cancelled.bin",
            ContentType = "application/octet-stream",
            Length = stream.Length
        };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => service.SaveFileAsync(file, "cancel-rooted", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SaveFileAsync_should_honor_cancellation_token_when_falling_back_to_current_directory()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var wwwrootPath = Path.Combine(currentDirectory, "wwwroot");

        try
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.WebRootPath).Returns(string.Empty);

            var service = new LocalFileStorageService(environment.Object);
            await using var stream = new MemoryStream(new byte[1024]);
            var file = new FileData
            {
                Content = stream,
                FileName = "cancelled.bin",
                ContentType = "application/octet-stream",
                Length = stream.Length
            };
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => service.SaveFileAsync(file, "cancel-fallback", cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            if (Directory.Exists(wwwrootPath))
            {
                Directory.Delete(wwwrootPath, recursive: true);
            }
        }
    }
}