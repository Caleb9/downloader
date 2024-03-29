using Api;
using Api.Notifications;
using Microsoft.Extensions.Options;

try
{
    using var host = CreateHostBuilder(args).Build();
    await host.RunAsync();
}
catch (Exception exception) when (
    exception
        is OptionsValidationException
        or DownloadDirectoriesCreator.DownloadPathAccessDeniedException)
{
    Console.WriteLine(exception.Message);
}

return;

static IHostBuilder CreateHostBuilder(
    string[] args)
{
    return Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
        /* Starts SignalR background service AFTER request pipeline is configured and application is started */
        .ConfigureServices(services =>
            services.AddHostedService<ProgressNotificationsBackgroundService>());
}