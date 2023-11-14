using System.Diagnostics;
using Api.Downloading.Directories;
using CSharpFunctionalExtensions;

namespace Api.Downloading;

[DebuggerDisplay("{" + nameof(Name) + "}")]
public sealed class SaveAsFile
{
    private readonly CompletedDownloadsDirectory _downloadsDirectory;
    private readonly int _seq;

    private SaveAsFile(
        CompletedDownloadsDirectory downloadsDirectory,
        string path,
        int seq = 0)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
        }

        _downloadsDirectory = downloadsDirectory;

        var subDir = SplitSubDir(path);
        if (!IsValidSubDir(subDir))
        {
            throw new ArgumentException($"{subDir} is not a valid subdirectory of {_downloadsDirectory}");
        }

        Directory = Path.Combine(_downloadsDirectory, subDir);

        var (nameWithoutExtension, extension) = SplitNameAndExtension(path);
        _seq = seq >= 0 ? seq : throw new ArgumentOutOfRangeException(nameof(seq), seq, "must not be negative");

        var fileName = _seq > 0
            ? $"{nameWithoutExtension}({_seq}){extension}"
            : $"{nameWithoutExtension}{extension}";
        Name = Path.Combine(subDir, fileName);
        FullName = Path.Combine(_downloadsDirectory, Name);
    }

    public string FullName { get; }

    public string Name { get; }

    public string Directory { get; }

    private static string SplitSubDir(string path)
    {
        var fileNameIndex = path.LastIndexOf(Path.DirectorySeparatorChar);
        return fileNameIndex > 1 ? path.Substring(0, fileNameIndex).Trim() : "";
    }

    /// <summary>
    ///     For simplicity we don't allow '..' and other kinds of values that could potentially formulate a valid
    ///     sub-directory path, but would be a pain to validate.
    /// </summary>
    private static bool IsValidSubDir(string subDirPath)
    {
        var invalidPathChars = Path.GetInvalidPathChars();
        return subDirPath.All(c => !invalidPathChars.Contains(c)) &&
               !Path.IsPathRooted(subDirPath) &&
               !Path.EndsInDirectorySeparator(subDirPath) &&
               !subDirPath.Contains("..") &&
               !subDirPath.EndsWith(".");
    }

    private static (string fileNameWithoutExtension, string extension) SplitNameAndExtension(
        string path)
    {
        var fileNameIndex = path.LastIndexOf(Path.DirectorySeparatorChar) + 1;
        var extensionIndex = path.LastIndexOf('.');
        if (extensionIndex < 1)
        {
            return (path.Substring(fileNameIndex).Trim(), string.Empty);
        }

        var fileNameWithoutExtension = path.Substring(fileNameIndex, extensionIndex - fileNameIndex);
        var extension = path.Substring(extensionIndex);
        return (fileNameWithoutExtension.Trim(), extension.Trim());
    }

    public static Result<SaveAsFile> Create(
        Link link,
        CompletedDownloadsDirectory downloadDirectoryPath,
        string fileName = "")
    {
        fileName = string.IsNullOrWhiteSpace(fileName) ? link.FileName : fileName;
        try
        {
            return new SaveAsFile(downloadDirectoryPath, fileName);
        }
        catch (ArgumentException e)
        {
            return Result.Failure<SaveAsFile>(e.Message);
        }
    }

    public SaveAsFile IncrementSequence()
    {
        return new SaveAsFile(
            _downloadsDirectory,
            Name,
            _seq + 1);
    }

    public static implicit operator string(SaveAsFile saveAsFile)
    {
        return saveAsFile.FullName;
    }
}