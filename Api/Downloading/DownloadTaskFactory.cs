using System;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading.Tasks;
using Api.Downloading.Directories;

namespace Api.Downloading
{
    public sealed class DownloadTaskFactory
    {
        private readonly IFileSystem _fileSystem;
        private readonly IncompleteDownloadsDirectory _incompleteDownloadsDirectory;

        public DownloadTaskFactory(
            IncompleteDownloadsDirectory incompleteDownloadsDirectory,
            IFileSystem fileSystem)
        {
            _incompleteDownloadsDirectory = incompleteDownloadsDirectory;
            _fileSystem = fileSystem;
        }

        internal async Task<SaveAsFile> CreateDownloadTask(
            Args args)
        {
            var (id, link, httpClient, setTotalBytes, setBytesDownloaded, saveAsFile) = args;

            using var response =
                await httpClient.GetAsync(link.Url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is not null)
            {
                setTotalBytes(response.Content.Headers.ContentLength.Value);
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            var temporaryFile = $"{_incompleteDownloadsDirectory}{id}";
            await using var temporaryFileStream =
                _fileSystem.FileStream.Create(temporaryFile, FileMode.CreateNew);

            await CopyResponseContentToTemporaryFile(responseStream, temporaryFileStream, setBytesDownloaded);
            return MoveTemporaryFileToSaveAsFile(temporaryFile, saveAsFile);
        }

        private async Task CopyResponseContentToTemporaryFile(
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

                await temporaryFileStream.WriteAsync(buffer.Slice(0, bytesRead));
            } while (bytesRead > 0);

            await temporaryFileStream.FlushAsync();
        }

        private SaveAsFile MoveTemporaryFileToSaveAsFile(
            string temporaryFile,
            SaveAsFile saveAsFile)
        {
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
}