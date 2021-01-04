using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Api;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Tests.Integration.Extensions;
using Xunit;

namespace Tests.Integration
{
    public sealed class Tests :
        IClassFixture<IntegrationTestWebApplicationFactory>
    {
        private const string ApiDownloadRoute = "/api/download";
        private readonly WebApplicationFactory<Startup> _factory;

        public Tests(
            IntegrationTestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public void Download_directories_get_created_on_startup()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var fileSystemMock = fixture.Create<Mock<IFileSystem>>();
            fileSystemMock
                .SetupGet(fs => fs.Path.DirectorySeparatorChar)
                .Returns('/');
            using var configuredFactory =
                _factory
                    .WithWebHostBuilder(builder =>
                        builder.ConfigureAppConfiguration(configBuilder =>
                            configBuilder.AddInMemoryCollection(
                                new Dictionary<string, string>
                                {
                                    {"DownloadDirectories:Incomplete", "/incomplete"},
                                    {"DownloadDirectories:Completed", "/completed"}
                                })))
                    .WithServices(services =>
                        services
                            .RemoveAll(typeof(IFileSystem))
                            .AddSingleton(fileSystemMock.Object));

            using var client = configuredFactory.CreateDefaultClient();

            using var _ = new AssertionScope();
            fileSystemMock.Verify(fs => fs.Directory.CreateDirectory("/incomplete/"));
            fileSystemMock.Verify(fs => fs.Directory.CreateDirectory("/completed/"));
        }

        private interface IProtectedDelegatingHandler
        {
            Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken);
        }
    }
}