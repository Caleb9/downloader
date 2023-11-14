using System.IO.Abstractions;
using AutoFixture;
using FluentAssertions.Execution;
using Moq;
using TestHelpers;
using Tests.Integration.Extensions;
using Xunit;

namespace Tests.Integration;

public sealed class StartupTest(
        IntegrationTestWebApplicationFactory factory)
    : IClassFixture<IntegrationTestWebApplicationFactory>
{
    [Fact]
    public void Directories_get_created_on_startup()
    {
        var fixture = new Fixture().Customize(new DownloaderCustomization());
        var fileSystemMock = fixture.Create<Mock<IFileSystem>>();
        using var configuredFactory =
            factory
                .WithServices(services =>
                    services
                        .ReplaceAllWithSingleton(fileSystemMock.Object))
                .WithSettings(
                    ("DownloadDirectories:Incomplete", "/incomplete"),
                    ("DownloadDirectories:Completed", "/completed"));

        using var client = configuredFactory.CreateDefaultClient();

        using var _ = new AssertionScope();
        fileSystemMock.Verify(fs => fs.Directory.CreateDirectory("/incomplete/"));
        fileSystemMock.Verify(fs => fs.Directory.CreateDirectory("/completed/"));
    }
}