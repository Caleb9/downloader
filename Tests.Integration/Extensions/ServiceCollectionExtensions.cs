using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

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

        internal static IServiceCollection ReplaceHttpMessageHandlerFor<TClient>(
            this IServiceCollection services,
            Mock<DelegatingHandler> fakeHttpMessageHandler)
            where TClient : class
        {
            return services.ReplaceHttpMessageHandlerFor<TClient>(fakeHttpMessageHandler.Object);
        }
    }
}