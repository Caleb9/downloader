namespace Api.Downloading.Directories;

public sealed record DirectorySeparatorChars(
    char Value = '/',
    char AltValue = '\\');