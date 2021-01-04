using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Api.Downloading.Directories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api
{
    public sealed class DownloadDirectoriesCreator
        : IHostedService
    {
        private readonly IncompleteDownloadsDirectory _incompleteDownloadsDirectory;
        private readonly CompletedDownloadsDirectory _completedDownloadsDirectory;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<DownloadDirectoriesCreator> _logger;

        public DownloadDirectoriesCreator(
            IncompleteDownloadsDirectory incompleteDownloadsDirectory,
            CompletedDownloadsDirectory completedDownloadsDirectory,
            IFileSystem fileSystem,
            ILogger<DownloadDirectoriesCreator> logger)
        {
            _incompleteDownloadsDirectory = incompleteDownloadsDirectory;
            _completedDownloadsDirectory = completedDownloadsDirectory;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            CreateDirectoryIfNotExistsAndCheckPermissions(_incompleteDownloadsDirectory);
            CreateDirectoryIfNotExistsAndCheckPermissions(_completedDownloadsDirectory);

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        private void CreateDirectoryIfNotExistsAndCheckPermissions(
            AbstractDownloadsDirectory directory)
        {
            try
            {
                CreateDirectory(directory);
            }
            catch (UnauthorizedAccessException)
            {
                throw new DownloadPathAccessDeniedException(
                    $"'{directory}' directory cannot be created");
            }

            try
            {
                AssertDownloadPathHasWriteAccess(directory);
            }
            catch (UnauthorizedAccessException)
            {
                throw new DownloadPathAccessDeniedException(
                    $"'{directory}' directory is not writable");
            }
        }

        private void CreateDirectory(
            AbstractDownloadsDirectory directory)
        {
            if (_fileSystem.Directory.Exists(directory) is false)
            {
                _logger.LogInformation($"Creating {directory} directory.");
            }
            /* If DownloadPath directory already exists this line does nothing. */
            _fileSystem.Directory.CreateDirectory(directory);
        }

        /// <summary>
        ///     Naive way to check if we can create files in DownloadPath. Standard .NET tools to perform such check are
        ///     currently only available on Windows. So we just try to create a random file and delete it - we'll get
        ///     an exception if that's not permitted. It is an unrecoverable situation so an exception is appropriate
        ///     here, as it will terminate the app.
        /// </summary>
        private void AssertDownloadPathHasWriteAccess(
            AbstractDownloadsDirectory directory)
        {
            var testFilePath =
                _fileSystem.Path.Combine(
                    directory,
                    $"downloaderPermissionsTest.{Guid.NewGuid()}");
            using (_fileSystem.File.Create(testFilePath))
            {
                /* Close and dispose the stream */
            }

            _fileSystem.File.Delete(testFilePath);
        }

        internal sealed class DownloadPathAccessDeniedException :
            Exception
        {
            public DownloadPathAccessDeniedException(string message)
                : base(message)
            {
            }
        }
    }
}