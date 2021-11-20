using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Api.Options;

public sealed class DownloadDirectoriesOptions
{
    internal const string Section = "DownloadDirectories";

    [Required(
        ErrorMessage = "DownloadDirectories:Incomplete option not set.")]
    public string Incomplete { get; [UsedImplicitly] init; } = string.Empty;

    [Required(
        ErrorMessage = "DownloadDirectories:Completed option not set.")]
    public string Completed { get; [UsedImplicitly] init; } = string.Empty;
}