using System.IO.Abstractions;
using Api.Downloading;
using Api.Downloading.Directories;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Kernel;
using NSubstitute;

namespace TestHelpers;

public sealed class DownloaderCustomization :
    ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture
            .Customize(new AutoNSubstituteCustomization())
            .Customizations.Add(new Generator());

        var fileSystemStreamStub = Substitute.For<FileSystemStream>(new MemoryStream(), "fake-path", true);
        fileSystemStreamStub
            .WriteAsync(default)
            .ReturnsForAnyArgs(ValueTask.CompletedTask);
        fixture.Inject(fileSystemStreamStub);

        var fileSystemStub = Substitute.For<IFileSystem>();
        fileSystemStub
            .FileStream.New(default!, (FileMode)default!)
            .ReturnsForAnyArgs(fileSystemStreamStub);

        fileSystemStub
            .File.Create(default!)
            .ReturnsForAnyArgs(fileSystemStreamStub);
        fixture.Inject(fileSystemStub);
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