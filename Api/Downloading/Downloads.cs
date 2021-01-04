using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using Api.Downloading.Directories;
using CSharpFunctionalExtensions;

namespace Api.Downloading
{
    public sealed class Downloads
    {
        private readonly IFileSystem _fileSystem;
        private readonly HttpClient _httpClient;
        private readonly IncompleteDownloadsDirectory _incompleteDownloadsDirectory;
        private readonly Func<Guid> _newGuid;

        private readonly ConcurrentDictionary<Guid, Download> _tasks;

        public Downloads(
            IncompleteDownloadsDirectory incompleteDownloadsDirectory,
            IFileSystem fileSystem,
            HttpClient httpClient,
            Func<Guid> newGuid,
            ConcurrentDictionary<Guid, Download> tasks)
        {
            _incompleteDownloadsDirectory = incompleteDownloadsDirectory;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
            _newGuid = newGuid;
            _tasks = tasks;
        }

        public Result<Guid> AddAndStart(
            Link link,
            SaveAsFile saveAsFile)
        {
            var id = _newGuid();
            var downloadTask =
                new Download(
                    id,
                    link,
                    _incompleteDownloadsDirectory,
                    saveAsFile);
            _tasks[id] = downloadTask;
            downloadTask.Start(
                _httpClient,
                _fileSystem);
            return id;
        }

        public Result<Download> Get(
            Guid id)
        {
            return
                _tasks.TryGetValue(id, out var download)
                    ? download
                    : Result.Failure<Download>("Not found");
        }

        public IReadOnlyCollection<Download> GetAll()
        {
            return _tasks.Values.ToList();
        }
    }
}