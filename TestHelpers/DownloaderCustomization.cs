using System;
using System.IO;
using System.IO.Abstractions;
using Api.Downloading;
using Api.Downloading.Directories;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using Moq;

namespace TestHelpers
{
    public sealed class DownloaderCustomization :
        ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture
                .Customize(new AutoMoqCustomization())
                .Customizations.Add(new Generator());

            fixture
                .Freeze<Mock<IFileSystem>>()
                .Setup(fs => fs.FileStream.Create(It.IsAny<string>(), It.IsAny<FileMode>()))
                .Returns(new MemoryStream());
        }

        private sealed class Generator :
            ISpecimenBuilder
        {
            public object Create(
                object request,
                ISpecimenContext context)
            {
                if (request is not Type type)
                {
                    return new NoSpecimen();
                }

                if (type == typeof(Link))
                {
                    return Link.Create(
                            $"https://{context.Create<string>()}/{context.Create<string>()}")
                        .Value;
                }

                if (type == typeof(SaveAsFile))
                {
                    return SaveAsFile.Create(
                            context.Create<Link>(),
                            context.Create<CompletedDownloadsDirectory>())
                        .Value;
                }

                return new NoSpecimen();
            }
        }
    }
}