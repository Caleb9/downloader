using System;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration.Extensions
{
    internal static class WebApplicationFactoryExtensions
    {
        internal static WebApplicationFactory<TStartup> WithServices<TStartup>(
            this WebApplicationFactory<TStartup> factory,
            Action<IServiceCollection> configureServices)
            where TStartup : class
        {
            return factory.WithWebHostBuilder(builder => builder.ConfigureServices(configureServices));
        }
    }
}