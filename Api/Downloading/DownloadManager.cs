namespace Api.Downloading;

public sealed class DownloadManager(
    DownloadManager.DownloadIdGenerator newGuid,
    DownloadManager.DateTimeUtcNowTicks getTicks,
    DownloadJobsDictionary jobs,
    DownloadTaskFactory downloadTaskFactory)
{
    public delegate long DateTimeUtcNowTicks();

    public delegate DownloadJob.JobId DownloadIdGenerator();

    internal DownloadJob CreateDownloadJob(
        Link link,
        SaveAsFile saveAsFile)
    {
        var id = newGuid();
        var job =
            new DownloadJob(
                id,
                link,
                saveAsFile,
                getTicks(),
                downloadTaskFactory);
        jobs[id] = job;
        return job;
    }

    internal void Cleanup()
    {
        var finishedStatuses = new[]
        {
            DownloadJob.DownloadStatus.Completed,
            DownloadJob.DownloadStatus.Failed
        };
        var finishedJobs = jobs.Where(d => finishedStatuses.Contains(d.Value.Status));
        foreach (var finishedJob in finishedJobs)
        {
            jobs.TryRemove(finishedJob);
        }
    }
}