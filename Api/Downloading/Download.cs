using System;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading.Tasks;
using Api.Downloading.Directories;
using CSharpFunctionalExtensions;

namespace Api.Downloading
{
    public sealed class Download
    {
        public enum DownloadStatus
        {
            NotStarted,
            Downloading,

            Completed
            /* TODO: These might be useful as well */
            // Failed
            // Paused
        }

        private readonly IncompleteDownloadsDirectory _incompleteDownloadsDirectory;
        private readonly Link _link;
        private readonly SaveAsFile _saveAsFile;

        private Task? _task;

        public Download(
            Guid id,
            Link link,
            IncompleteDownloadsDirectory incompleteDownloadsDirectory,
            SaveAsFile saveAsFile)
        {
            Id = id;
            _link = link;
            _incompleteDownloadsDirectory = incompleteDownloadsDirectory;
            _saveAsFile = saveAsFile;
        }

        public Guid Id { get; }

        public DownloadStatus Status
        {
            get
            {
                if (_task is null)
                {
                    return DownloadStatus.NotStarted;
                }

                return _task.IsCompleted ? DownloadStatus.Completed : DownloadStatus.Downloading;
            }
        }

        public Result Start(
            HttpClient httpClient,
            IFileSystem fileSystem)
        {
            if (_task is not null)
            {
                return Result.Failure("Already downloading");
            }

            _task = StartDownload(httpClient, fileSystem);
            return Result.Success();
        }

        private async Task StartDownload(
            HttpClient httpClient,
            IFileSystem fileSystem)
        {
            var temporaryFile = $"{_incompleteDownloadsDirectory}{Id}";
            await Download();
            MoveToSaveAsFile();

            async Task Download()
            {
                using var response = await httpClient.GetAsync(_link.Url, HttpCompletionOption.ResponseHeadersRead);
                // TODO progress
                await using var temporaryFileStream =
                    fileSystem.FileStream.Create(temporaryFile, FileMode.CreateNew);
                await response.Content.CopyToAsync(temporaryFileStream);
            }

            void MoveToSaveAsFile()
            {
                var saveAsFile = _saveAsFile;
                while (true)
                {
                    try
                    {
                        fileSystem.File.Move(temporaryFile, saveAsFile, false);
                        break;
                    }
                    catch (IOException)
                    {
                        saveAsFile = saveAsFile.IncrementSequence();
                    }
                }
            }
        }
    }
}