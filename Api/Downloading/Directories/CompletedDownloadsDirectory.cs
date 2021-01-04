using System.IO.Abstractions;
using Api.Options;
using Microsoft.Extensions.Options;

namespace Api.Downloading.Directories
{
    public sealed class CompletedDownloadsDirectory :
        AbstractDownloadsDirectory
    {
        public CompletedDownloadsDirectory(
            IFileSystem fileSystem,
            IOptions<DownloadOptions> options)
            : base(fileSystem, options.Value.Completed)
        {
        }
    }
}