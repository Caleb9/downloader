using Api.Options;

namespace Api.Downloading.Directories;

public sealed class IncompleteDownloadsDirectory(
        DownloadDirectoriesOptions options,
        DirectorySeparatorChars directorySeparatorChars)
    : AbstractDownloadsDirectory(options.Incomplete, directorySeparatorChars);