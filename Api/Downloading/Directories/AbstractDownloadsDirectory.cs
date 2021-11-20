namespace Api.Downloading.Directories;

public abstract class AbstractDownloadsDirectory
{
    private readonly string _path;

    protected AbstractDownloadsDirectory(
        string path,
        DirectorySeparatorChars directorySeparatorChars)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
        }

        var (dirSeparator, altDirSeparator) = directorySeparatorChars;
        _path =
            path
                .TrimEnd(dirSeparator)
                .TrimEnd(altDirSeparator)
            + dirSeparator;
    }

    public static implicit operator string(
        AbstractDownloadsDirectory directory)
    {
        return directory._path;
    }

    public override string ToString()
    {
        return _path;
    }
}