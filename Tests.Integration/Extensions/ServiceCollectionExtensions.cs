using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tests.Integration.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        internal static IServiceCollection ReplaceHttpMessageHandlerFor<TClient>(
            this IServiceCollection services,
            DelegatingHandler fakeHttpMessageHandler)
            where TClient : class
        {
            services
                .AddHttpClient<TClient>()
                .ConfigurePrimaryHttpMessageHandler(() => fakeHttpMessageHandler);
            return services;
        }

        internal static IServiceCollection ReplaceAllWithSingleton<T>(
            this IServiceCollection services,
            T instance)
            where T : class
        {
            return services
                .RemoveAll<T>()
                .AddSingleton(instance);
        }
    }
}