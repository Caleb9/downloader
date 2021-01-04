using System;
using System.Diagnostics;
using Api.Downloading.Directories;
using CSharpFunctionalExtensions;

namespace Api.Downloading
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public sealed class SaveAsFile
    {
        private readonly CompletedDownloadsDirectory _downloadsDirectory;
        private readonly string _nameWithoutExtension;
        private readonly string _extension;
        private readonly int _seq;

        private SaveAsFile(
            CompletedDownloadsDirectory downloadsDirectory,
            string fileName,
            int seq = 0)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
            }

            _downloadsDirectory = downloadsDirectory;

            (_nameWithoutExtension, _extension) = SplitNameAndExtension(fileName);
            _seq = seq >= 0 ? seq : throw new ArgumentOutOfRangeException(nameof(seq), seq, "must not be negative");

            Name = _seq > 0
                ? $"{_nameWithoutExtension}({_seq}){_extension}"
                : $"{_nameWithoutExtension}{_extension}";
            FullName = $"{_downloadsDirectory}{Name}";
        }

        public string FullName { get; }

        public string Name { get; }

        private static (string fileNameWithoutExtension, string extension) SplitNameAndExtension(
            string fileName)
        {
            var extensionIndex = fileName.LastIndexOf('.');
            if (extensionIndex < 1)
            {
                return (fileName, string.Empty);
            }

            var fileNameWithoutExtension = fileName.Substring(0, extensionIndex);
            var extension = fileName.Substring(extensionIndex);
            return (fileNameWithoutExtension, extension);
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
            return new(
                _downloadsDirectory,
                _nameWithoutExtension + _extension,
                _seq + 1);
        }

        public static implicit operator string(SaveAsFile saveAsFile)
        {
            return saveAsFile.FullName;
        }
    }
}