using Api.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Api.Notifications;

internal sealed class ProgressNotificationsBackgroundService :
    BackgroundService
{
    private readonly int _intervalInSeconds;
    private readonly IHubContext<NotificationsHub, NotificationsHub.IClient> _progressHub;
    private readonly ProgressNotificationDictionary _progressNotifications;

    public ProgressNotificationsBackgroundService(
        IHubContext<NotificationsHub, NotificationsHub.IClient> progressHub,
        ProgressNotificationDictionary progressNotifications,
        IOptions<PushNotificationsOptions> options)
    {
        _progressHub = progressHub;
        _progressNotifications = progressNotifications;
        _intervalInSeconds = options.Value.ProgressIntervalInMilliseconds;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested is false)
        {
            var progressNotifications =
                _progressNotifications
                    .Select(n => new NotificationsHub.ProgressMessage(n.Key, n.Value))
                    .ToList( /* Take snapshot of current dictionary contents. */);
            if (progressNotifications.Any())
            {
                /* There's a risk of low-impact race condition here. Contents of the _progressNotifications may have
                 * changed since we read them, so we might clear progress messages that we're not sending in this
                 * iteration. However missing one or several progress messages is not critical, while it
                 * significantly simplifies the implementation. Otherwise we'd need to introduce a "locked"
                 * method that removes multiple entries from the dictionary. */
                _progressNotifications.Clear();
                await _progressHub.Clients.All.SendProgress(
                    progressNotifications,
                    stoppingToken);
            }

            await Task.Delay(_intervalInSeconds, stoppingToken);
        }
    }
}