using System.IO.Abstractions;
using System.Net;
using Api.Downloading.Directories;
using CSharpFunctionalExtensions;

namespace Api.Downloading;

public sealed class DownloadTaskFactory
{
    private readonly IFileSystem _fileSystem;
    private readonly IncompleteDownloadsDirectory _incompleteDownloadsDirectory;

    private readonly HttpStatusCode[] _redirectHttpStatuses =
    {
        HttpStatusCode.Moved,
        HttpStatusCode.Redirect,
        HttpStatusCode.TemporaryRedirect,
        HttpStatusCode.PermanentRedirect
    };

    public DownloadTaskFactory(
        IncompleteDownloadsDirectory incompleteDownloadsDirectory,
        IFileSystem fileSystem)
    {
        _incompleteDownloadsDirectory = incompleteDownloadsDirectory;
        _fileSystem = fileSystem;
    }

    internal async Task<Result<SaveAsFile>> CreateDownloadTask(
        Args args)
    {
        var (id, link, httpClient, setTotalBytes, setBytesDownloaded, saveAsFile) = args;

        using var response = await httpClient.GetAsync(link.Url, HttpCompletionOption.ResponseHeadersRead);
        if (ResponseIsRedirect(response))
        {
            return await CreateRedirectedDownloadTask(response, args);
        }

        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is not null)
        {
            setTotalBytes(response.Content.Headers.ContentLength.Value);
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var temporaryFile = $"{_incompleteDownloadsDirectory}{id}";
        await using var temporaryFileStream = _fileSystem.FileStream.New(temporaryFile, FileMode.CreateNew);

        await CopyResponseContentToTemporaryFile(responseStream, temporaryFileStream, setBytesDownloaded);
        return MoveTemporaryFileToSaveAsFile(temporaryFile, saveAsFile);
    }

    private bool ResponseIsRedirect(
        HttpResponseMessage response)
    {
        return _redirectHttpStatuses.Contains(response.StatusCode);
    }

    private async Task<Result<SaveAsFile>> CreateRedirectedDownloadTask(
        HttpResponseMessage redirectResponse,
        Args args)
    {
        if (string.IsNullOrWhiteSpace(redirectResponse.Headers.Location?.OriginalString))
        {
            return Result.Failure<SaveAsFile>($"{args.Link} redirects to undefined location.");
        }

        var newLinkResult = Link.Create(redirectResponse.Headers.Location.OriginalString);
        if (newLinkResult.IsFailure)
        {
            return Result.Failure<SaveAsFile>(newLinkResult.Error);
        }

        var (id, _, httpClient, setTotalBytes, setBytesDownloaded, saveAsFile) = args;
        return await CreateDownloadTask(
            new Args(
                id,
                newLinkResult.Value,
                httpClient,
                setTotalBytes,
                setBytesDownloaded,
                saveAsFile));
    }

    private static async Task CopyResponseContentToTemporaryFile(
        Stream responseStream,
        Stream temporaryFileStream,
        SetBytesDownloaded setBytesDownloaded)
    {
        var buffer = new Memory<byte>(new byte[8192]);
        long totalBytesDownloaded = 0;
        int bytesRead;
        do
        {
            bytesRead = await responseStream.ReadAsync(buffer);
            totalBytesDownloaded += bytesRead;
            setBytesDownloaded(totalBytesDownloaded);

            await temporaryFileStream.WriteAsync(buffer[..bytesRead]);
        } while (bytesRead > 0);

        await temporaryFileStream.FlushAsync();
    }

    private SaveAsFile MoveTemporaryFileToSaveAsFile(
        string temporaryFile,
        SaveAsFile saveAsFile)
    {
        _fileSystem.Directory.CreateDirectory(saveAsFile.Directory);
        while (true)
        {
            try
            {
                const bool overwrite = false;
                _fileSystem.File.Move(temporaryFile, saveAsFile, overwrite);
                return saveAsFile;
            }
            catch (IOException)
            {
                saveAsFile = saveAsFile.IncrementSequence();
            }
        }
    }

    internal delegate void SetTotalBytes(long totalBytes);

    internal delegate void SetBytesDownloaded(long bytesDownloaded);

    internal sealed record Args(
        DownloadJob.JobId Id,
        Link Link,
        HttpClient HttpClient,
        SetTotalBytes SetTotalBytes,
        SetBytesDownloaded SetTotalBytesRead,
        SaveAsFile SaveAsFile);
}