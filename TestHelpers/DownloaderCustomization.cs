using System.IO.Abstractions;
using Api.Downloading;
using Api.Downloading.Directories;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using Moq;

namespace TestHelpers;

public sealed class DownloaderCustomization :
    ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture
            .Customize(new AutoMoqCustomization())
            .Customizations.Add(new Generator());

        var fileSystemStreamStub = fixture.Freeze<Mock<FileSystemStream>>();
        fileSystemStreamStub
            .Setup(s => s.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var fileSystemStub = fixture.Freeze<Mock<IFileSystem>>();
        fileSystemStub
            .Setup(fs => fs.FileStream.New(It.IsAny<string>(), It.IsAny<FileMode>()))
            /* Returns fileSystemStreamStub.Object */
            .ReturnsUsingFixture(fixture);
        fileSystemStub
            .Setup(fs => fs.File.Create(It.IsAny<string>()))
            .ReturnsUsingFixture(fixture);
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