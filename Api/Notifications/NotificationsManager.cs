using Api.Downloading;
using Microsoft.AspNetCore.SignalR;

namespace Api.Notifications;

public sealed class NotificationsManager
{
    private readonly IHubContext<NotificationsHub, NotificationsHub.IClient> _hubContext;
    private readonly ILogger<NotificationsManager> _logger;
    private readonly ProgressNotificationDictionary _progressNotifications;

    public NotificationsManager(
        IHubContext<NotificationsHub, NotificationsHub.IClient> hubContext,
        ProgressNotificationDictionary progressNotifications,
        ILogger<NotificationsManager> logger)
    {
        _hubContext = hubContext;
        _progressNotifications = progressNotifications;
        _logger = logger;
    }

    internal DownloadJob AddNotificationEventHandlers(
        DownloadJob job)
    {
        job.OnTotalBytesRecorded +=
            async (_, args) => await HandleTotalBytesRecorded(args);
        job.OnFinished +=
            async (_, args) => await HandleFinished(args);
        job.OnFailed +=
            async (_, args) => await HandleFailed(args);
        /* This event is treated differently because we don't send messages immediately. Instead, they get
         * sent in bulk via ProgressNotificationsBackgroundService*/
        job.OnProgress +=
            (_, args) => _progressNotifications[args.Id] = args.TotalBytesRead;
        return job;
    }

    private async Task HandleTotalBytesRecorded(
        DownloadJob.TotalBytesRecordedEventArgs args)
    {
        var (id, totalBytes) = args;
        try
        {
            await _hubContext.Clients.All.SendTotalBytes(
                new NotificationsHub.TotalBytesMessage(id, totalBytes));
        }
        catch (Exception exception)
        {
            LogError<NotificationsHub.TotalBytesMessage>(exception, id);
        }
    }

    private async Task HandleFinished(DownloadJob.FinishedEventArgs args)
    {
        var (id, savedAsFile) = args;
        try
        {
            await _hubContext.Clients.All.SendFinished(
                new NotificationsHub.FinishedMessage(id, savedAsFile.Name));
        }
        catch (Exception exception)
        {
            LogError<NotificationsHub.FinishedMessage>(exception, id);
        }
    }

    private async Task HandleFailed(DownloadJob.FailedEventArgs args)
    {
        var (id, reason) = args;
        try
        {
            await _hubContext.Clients.All.SendFailed(
                new NotificationsHub.FailedMessage(id, reason));
        }
        catch (Exception exception)
        {
            LogError<NotificationsHub.FinishedMessage>(exception, id);
        }
    }

    /// <summary>
    ///     Async event handlers are not awaited so any exceptions thrown are lost. There's nothing more we can do
    ///     but to log them.
    /// </summary>
    private void LogError<TMessage>(
        Exception exception,
        DownloadJob.JobId id)
    {
        _logger.LogError(
            exception,
            "Failed to send {messageType} for job {id}",
            typeof(TMessage).Name, id);
    }
}