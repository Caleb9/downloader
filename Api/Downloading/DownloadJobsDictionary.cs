using System.Collections.Concurrent;

namespace Api.Downloading;

public sealed class DownloadJobsDictionary
    : ConcurrentDictionary<DownloadJob.JobId, DownloadJob>;