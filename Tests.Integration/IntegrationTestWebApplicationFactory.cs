using System.IO.Abstractions;
using System.Net.Http;
using Api;
using Api.Downloading;
using AutoFixture;
using AutoFixture.AutoMoq;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Tests.Integration.Extensions;

namespace Tests.Integration
{
    [UsedImplicitly]
    public sealed class IntegrationTestWebApplicationFactory :
        WebApplicationFactory<Startup>
    {
        private static readonly IFixture Fixture = new Fixture().Customize(new AutoMoqCustomization());

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(ReplaceDependenciesAccessingOutOfProcessResources);
        }

        private static void ReplaceDependenciesAccessingOutOfProcessResources(
            IServiceCollection services)
        {
            services
                .RemoveAll(typeof(IFileSystem))
                .AddSingleton(Fixture.Create<Mock<IFileSystem>>());

            services.ReplaceHttpMessageHandlerFor<Downloads>(Fixture.Create<DelegatingHandler>());
        }
    }
}