using System;
using Api.Notifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Api
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using var host = CreateHostBuilder(args).Build();
                host.Run();
            }
            catch (Exception exception) when (
                exception is OptionsValidationException ||
                exception is DownloadDirectoriesCreator.DownloadPathAccessDeniedException)
            {
                Console.WriteLine(exception.Message);
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
                /* Starts SignalR background service AFTER request pipeline is configured and application is started */
                .ConfigureServices(services =>
                    services.AddHostedService<ProgressNotificationsBackgroundService>());
        }
    }
}