using Api.Options;

namespace Api.Downloading.Directories;

public sealed class IncompleteDownloadsDirectory :
    AbstractDownloadsDirectory
{
    public IncompleteDownloadsDirectory(
        DownloadDirectoriesOptions options,
        DirectorySeparatorChars directorySeparatorChars)
        : base(options.Incomplete, directorySeparatorChars)
    {
    }
}