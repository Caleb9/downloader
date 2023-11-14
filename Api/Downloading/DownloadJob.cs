using CSharpFunctionalExtensions;

namespace Api.Downloading;

public sealed class DownloadJob(
    DownloadJob.JobId id,
    Link link,
    SaveAsFile saveAsFile,
    long createdTicks,
    DownloadTaskFactory downloadTaskFactory)
{
    public enum DownloadStatus
    {
        NotStarted,
        Downloading,
        Completed,
        Failed

        // TODO Paused
    }

    private const long UnknownContentLength = -1;

    private long _bytesDownloaded;

    private Task? _downloadTask;

    private string _reasonForFailure = string.Empty;

    private long _totalBytes = UnknownContentLength;

    public JobId Id { get; } = id;
    public Link Link { get; } = link;
    public SaveAsFile SaveAsFile { get; private set; } = saveAsFile;
    public long CreatedTicks { get; } = createdTicks;

    public Task DownloadTask =>
        _downloadTask ?? throw new InvalidOperationException("Download not started.");

    public long TotalBytes
    {
        get => _totalBytes;
        private set
        {
            if (value <= 0)
            {
                return;
            }

            _totalBytes = value;
            OnTotalBytesRecorded(this, new TotalBytesRecordedEventArgs(Id, value));
        }
    }

    public long BytesDownloaded
    {
        get => _bytesDownloaded;
        private set
        {
            if (value <= 0)
            {
                return;
            }

            _bytesDownloaded = value;
            OnProgress(this, new ProgressEventArgs(Id, value));
        }
    }

    public DownloadStatus Status
    {
        get
        {
            if (_downloadTask is null)
            {
                return DownloadStatus.NotStarted;
            }

            if (_downloadTask.IsCompletedSuccessfully)
            {
                return DownloadStatus.Completed;
            }

            if (_downloadTask.IsFaulted)
            {
                return DownloadStatus.Failed;
            }

            return DownloadStatus.Downloading;
        }
    }

    public string ReasonForFailure
    {
        get => _reasonForFailure;
        private set
        {
            _reasonForFailure = value;
            OnFailed(this, new FailedEventArgs(Id, value));
        }
    }


    public event EventHandler<TotalBytesRecordedEventArgs> OnTotalBytesRecorded = (_, _) => { };
    public event EventHandler<ProgressEventArgs> OnProgress = (_, _) => { };
    public event EventHandler<FinishedEventArgs> OnFinished = (_, _) => { };
    public event EventHandler<FailedEventArgs> OnFailed = (_, _) => { };


    public Result Start(
        HttpClient httpClient)
    {
        if (_downloadTask is not null)
        {
            return Result.Failure("Already started");
        }

        _downloadTask = StartDownloadTask(httpClient);
        return Result.Success();
    }

    private async Task StartDownloadTask(
        HttpClient httpClient)
    {
        try
        {
            var saveAsFileResult = await downloadTaskFactory.CreateDownloadTask(
                new DownloadTaskFactory.Args(
                    Id,
                    Link,
                    httpClient,
                    totalBytes => TotalBytes = totalBytes,
                    bytesDownloaded => BytesDownloaded = bytesDownloaded,
                    SaveAsFile));
            if (saveAsFileResult.IsFailure)
            {
                ReasonForFailure = saveAsFileResult.Error;
                return;
            }

            SaveAsFile = saveAsFileResult.Value;
            OnFinished(this, new FinishedEventArgs(Id, SaveAsFile));
            _downloadTask = Task.CompletedTask;
        }
        catch (Exception exception)
        {
            ReasonForFailure = exception.Message;
            _downloadTask = Task.FromException(exception);
            throw;
        }
        finally
        {
            ReleaseEventHandlers();
        }
    }

    private void ReleaseEventHandlers()
    {
        OnTotalBytesRecorded = (_, _) => { };
        OnProgress = (_, _) => { };
        OnFinished = (_, _) => { };
        OnFailed = (_, _) => { };
    }

    public sealed record JobId(Guid Value)
    {
        public static implicit operator Guid(JobId id)
        {
            return id.Value;
        }

        public static implicit operator string(JobId id)
        {
            return id.Value.ToString();
        }

        public override string ToString()
        {
            return this;
        }
    }

    public sealed record TotalBytesRecordedEventArgs(JobId Id, long TotalBytes);

    public sealed record ProgressEventArgs(JobId Id, long TotalBytesRead);

    public sealed record FinishedEventArgs(JobId Id, SaveAsFile SavedAsFile);

    public sealed record FailedEventArgs(JobId Id, string Reason);
}