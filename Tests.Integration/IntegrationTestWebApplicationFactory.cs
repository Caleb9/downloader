using System.IO.Abstractions;
using Api;
using Api.Downloading;
using Api.Downloading.Directories;
using AutoFixture;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers;
using Tests.Integration.Extensions;

namespace Tests.Integration;

[UsedImplicitly]
public sealed class IntegrationTestWebApplicationFactory
    : WebApplicationFactory<Startup>
{
    private static readonly IFixture Fixture = new Fixture().Customize(new DownloaderCustomization());

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(ReplaceDependenciesAccessingOutOfProcessResources);
        builder.ConfigureAppConfiguration(DisableLogging);
    }

    private static void ReplaceDependenciesAccessingOutOfProcessResources(
        IServiceCollection services)
    {
        services
            .ReplaceAllWithSingleton(Fixture.Create<IFileSystem>())
            /* Make this independent of the OS when running tests */
            .ReplaceAllWithSingleton(new DirectorySeparatorChars())
            .ReplaceHttpMessageHandlerFor<DownloadStarter>(Fixture.Create<DelegatingHandler>());
    }

    private static void DisableLogging(
        IConfigurationBuilder configBuilder)
    {
        configBuilder.AddInMemoryCollection(
            new[] { new KeyValuePair<string, string?>("Logging:LogLevel:Default", "None") });
    }
}