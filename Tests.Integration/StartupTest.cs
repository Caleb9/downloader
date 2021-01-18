using System.IO.Abstractions;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions.Execution;
using Moq;
using Tests.Integration.Extensions;
using Xunit;

namespace Tests.Integration
{
    public sealed class StartupTest :
        IClassFixture<IntegrationTestWebApplicationFactory>
    {
        private readonly IntegrationTestWebApplicationFactory _factory;

        public StartupTest(
            IntegrationTestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public void Directories_get_created_on_startup()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var fileSystemMock = fixture.Create<Mock<IFileSystem>>();
            using var configuredFactory =
                _factory
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
}