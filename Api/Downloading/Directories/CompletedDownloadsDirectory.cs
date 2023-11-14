using Api.Options;

namespace Api.Downloading.Directories;

public sealed class CompletedDownloadsDirectory(
        DownloadDirectoriesOptions options,
        DirectorySeparatorChars directorySeparatorChars)
    : AbstractDownloadsDirectory(options.Completed, directorySeparatorChars);