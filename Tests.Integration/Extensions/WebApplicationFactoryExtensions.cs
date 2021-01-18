using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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
            return factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(configureServices));
        }

        internal static WebApplicationFactory<TStartup> WithSettings<TStartup>(
            this WebApplicationFactory<TStartup> factory,
            params (string key, string value)[] settings)
            where TStartup : class
        {
            return factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration(configBuilder =>
                    configBuilder.AddInMemoryCollection(
                        settings.Select(tuple =>
                            new KeyValuePair<string, string>(tuple.key, tuple.value)))));
        }
    }
}