using System.IO.Abstractions;
using Api.Downloading.Directories;

namespace Api;

internal sealed class DownloadDirectoriesCreator(
        IncompleteDownloadsDirectory incompleteDownloadsDirectory,
        CompletedDownloadsDirectory completedDownloadsDirectory,
        IFileSystem fileSystem,
        ILogger<DownloadDirectoriesCreator> logger)
    : IHostedService
{
    async Task IHostedService.StartAsync(
        CancellationToken cancellationToken)
    {
        CreateDirectoryIfNotExistsAndCheckPermissions(incompleteDownloadsDirectory);
        CreateDirectoryIfNotExistsAndCheckPermissions(completedDownloadsDirectory);

        await Task.CompletedTask;
    }

    async Task IHostedService.StopAsync(
        CancellationToken cancellationToken)
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
        if (fileSystem.Directory.Exists(directory) is false)
        {
            logger.LogInformation($"Creating {directory} directory.");
        }

        /* If DownloadPath directory already exists this line does nothing. */
        fileSystem.Directory.CreateDirectory(directory);
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
            fileSystem.Path.Combine(
                directory,
                $"downloaderPermissionsTest.{Guid.NewGuid()}");
        using (fileSystem.File.Create(testFilePath))
        {
            /* Close and dispose the stream */
        }

        fileSystem.File.Delete(testFilePath);
    }

    internal sealed class DownloadPathAccessDeniedException(
            string message)
        : Exception(message);
}