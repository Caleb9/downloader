namespace Api.Downloading
{
    public sealed class DownloadManager
    {
        public delegate long DateTimeUtcNowTicks();

        public delegate DownloadJob.JobId DownloadIdGenerator();

        private readonly DownloadTaskFactory _downloadTaskFactory;
        private readonly DateTimeUtcNowTicks _getTicks;
        private readonly DownloadJobsDictionary _jobs;
        private readonly DownloadIdGenerator _newGuid;

        public DownloadManager(
            DownloadIdGenerator newGuid,
            DateTimeUtcNowTicks getTicks,
            DownloadJobsDictionary jobs,
            DownloadTaskFactory downloadTaskFactory)
        {
            _newGuid = newGuid;
            _getTicks = getTicks;
            _jobs = jobs;
            _downloadTaskFactory = downloadTaskFactory;
        }

        internal DownloadJob CreateDownloadJob(
            Link link,
            SaveAsFile saveAsFile)
        {
            var id = _newGuid();
            var job =
                new DownloadJob(
                    id,
                    link,
                    saveAsFile,
                    _getTicks(),
                    _downloadTaskFactory);
            _jobs[id] = job;
            return job;
        }
    }
}