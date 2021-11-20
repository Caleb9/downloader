using Api.Options;

namespace Api.Downloading.Directories;

public sealed class CompletedDownloadsDirectory :
    AbstractDownloadsDirectory
{
    public CompletedDownloadsDirectory(
        DownloadDirectoriesOptions options,
        DirectorySeparatorChars directorySeparatorChars)
        : base(options.Completed, directorySeparatorChars)
    {
    }
}