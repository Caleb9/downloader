using System;
using System.IO.Abstractions;

namespace Api.Downloading.Directories
{
    public abstract class AbstractDownloadsDirectory
    {
        private readonly string _path;

        protected AbstractDownloadsDirectory(
            IFileSystem fileSystem,
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            _path =
                path
                    .TrimEnd(fileSystem.Path.DirectorySeparatorChar)
                    .TrimEnd(fileSystem.Path.AltDirectorySeparatorChar)
                + fileSystem.Path.DirectorySeparatorChar;
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
}