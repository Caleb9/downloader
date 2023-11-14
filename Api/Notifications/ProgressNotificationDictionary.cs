using System.Collections.Concurrent;
using Api.Downloading;

namespace Api.Notifications;

public sealed class ProgressNotificationDictionary
    : ConcurrentDictionary<DownloadJob.JobId, long>;