using System.IO.Abstractions;
using System.Net;
using System.Reflection;
using System.Text;
using Api.Downloading;
using AutoFixture;
using FluentAssertions;
using NSubstitute;
using TestHelpers;
using Xunit;

namespace Tests.Unit.Downloading;

public class DownloadJobTests
{
    private static IFixture NewFixture()
    {
        return new Fixture().Customize(new DownloaderCustomization());
    }

    [Fact]
    public void Status_is_NotStarted_when_job_is_not_started()
    {
        var fixture = NewFixture();
        var sut = fixture.Create<DownloadJob>();

        var result = sut.Status;

        result.Should().Be(DownloadJob.DownloadStatus.NotStarted);
    }

    [Fact]
    public void Status_is_Downloading_when_download_task_is_started_but_not_completed()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        using var httpClient = new HttpClient(NewForeverRunningHttpMessageHandler(link.Url));

        var result = sut.Start(httpClient);

        result.IsSuccess.Should().BeTrue();
        sut.Status.Should().Be(DownloadJob.DownloadStatus.Downloading);
    }

    [Fact]
    public async Task Status_is_Completed_when_download_task_is_completed()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        using var httpClient =
            new HttpClient(
                NewDefaultHttpMessageHandler(
                    link.Url));
        sut.Start(httpClient);

        await sut.DownloadTask;

        sut.Status.Should().Be(DownloadJob.DownloadStatus.Completed);
    }

    [Fact]
    public async Task Status_is_Failed_when_download_task_throws()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        using var httpClient = new HttpClient(NewFailingHttpMessageHandler(link.Url));
        sut.Start(httpClient);

        await sut.DownloadTask.AwaitIgnoringExceptions();

        sut.Status.Should().Be(DownloadJob.DownloadStatus.Failed);
    }


    [Fact]
    public void Start_returns_failure_when_job_is_already_started()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        using var httpClient = new HttpClient(NewForeverRunningHttpMessageHandler(link.Url));

        sut.Start(httpClient);

        /* Act */
        var result = sut.Start(Substitute.For<HttpClient>());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Start_returns_success_when_job_is_not_yet_started()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        using var httpClient = new HttpClient(NewForeverRunningHttpMessageHandler(link.Url));

        /* Act */
        var result = sut.Start(httpClient);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task TotalBytes_is_fetched_from_HttpClient()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        const string content = "file contents";
        using var httpClient =
            new HttpClient(
                NewDefaultHttpMessageHandler(
                    link.Url,
                    new HttpResponseMessage
                    {
                        Content = new StringContent(content)
                    }));
        sut.Start(httpClient);

        await sut.DownloadTask;

        sut.TotalBytes.Should().Be(content.Length);
    }

    [Fact]
    public async Task TotalBytes_is_negative_when_response_does_not_provide_it()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        using var httpClient =
            new HttpClient(
                NewDefaultHttpMessageHandler(
                    link.Url,
                    new HttpResponseMessage
                    {
                        Content = new StreamContent(new MemoryStream())
                    }));
        sut.Start(httpClient);

        await sut.DownloadTask;

        sut.TotalBytes.Should().BeLessThan(0);
    }

    [Fact]
    public async Task OnTotalBytesRecorded_is_invoked_when_response_content_length_is_fetched()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        DownloadJob.TotalBytesRecordedEventArgs? eventArgs = null;
        sut.OnTotalBytesRecorded += (_, args) => eventArgs = args;
        const string content = "file contents";
        using var httpClient =
            new HttpClient(
                NewDefaultHttpMessageHandler(
                    link.Url,
                    new HttpResponseMessage
                    {
                        Content = new StringContent(content)
                    }));
        sut.Start(httpClient);

        await sut.DownloadTask;

        eventArgs.Should().NotBeNull();
        eventArgs!.Id.Should().Be(sut.Id);
        eventArgs.TotalBytes.Should().Be(content.Length);
    }

    [Fact]
    public async Task OnTotalBytesRecorded_is_not_invoked_when_response_content_does_not_provide_length()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        var callbackInvoked = false;
        sut.OnTotalBytesRecorded += (_, _) => callbackInvoked = true;
        using var httpClient =
            new HttpClient(
                NewDefaultHttpMessageHandler(
                    link.Url,
                    new HttpResponseMessage
                    {
                        Content = new StreamContent(new MemoryStream())
                    }));
        sut.Start(httpClient);

        await sut.DownloadTask;

        callbackInvoked.Should().BeFalse();
    }

    [Fact]
    public async Task OnProgress_is_invoked_when_chunk_of_content_is_copied()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        var callbackArgsQueue = new Queue<DownloadJob.ProgressEventArgs>();
        sut.OnProgress += (_, args) => { callbackArgsQueue.Enqueue(args); };
        var content = fixture.CreateLongString();
        using var httpClient =
            new HttpClient(
                NewDefaultHttpMessageHandler(
                    link.Url,
                    new HttpResponseMessage
                    {
                        Content = new StringContent(content)
                    }));
        sut.Start(httpClient);

        await sut.DownloadTask;

        callbackArgsQueue.Should().NotBeEmpty();
        callbackArgsQueue.Should().OnlyContain(args => args.Id == sut.Id);
        callbackArgsQueue.Should().BeInAscendingOrder(args => args.TotalBytesRead);
        callbackArgsQueue.Last().TotalBytesRead.Should().Be(content.Length);
    }

    [Fact]
    public async Task OnFinished_is_invoked_when_download_is_finished_successfully()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        DownloadJob.FinishedEventArgs? callbackArgs = null;
        sut.OnFinished += (_, args) => { callbackArgs = args; };
        var content = fixture.CreateLongString();
        using var httpClient =
            new HttpClient(
                NewDefaultHttpMessageHandler(
                    link.Url,
                    new HttpResponseMessage
                    {
                        Content = new StringContent(content)
                    }));
        sut.Start(httpClient);

        await sut.DownloadTask;

        callbackArgs.Should().NotBeNull();
        callbackArgs!.Id.Should().Be(sut.Id);
    }

    [Fact]
    public async Task OnFailed_is_invoked_when_download_is_failed()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        DownloadJob.FailedEventArgs? callbackArgs = null;
        sut.OnFailed += (_, args) => { callbackArgs = args; };
        using var httpClient =
            new HttpClient(
                NewFailingHttpMessageHandler(
                    link.Url,
                    new Exception("Something went wrong!")));
        sut.Start(httpClient);

        await sut.DownloadTask.AwaitIgnoringExceptions();

        callbackArgs.Should().NotBeNull();
        callbackArgs!.Id.Should().Be(sut.Id);
        callbackArgs.Reason.Should().Be("Something went wrong!");
    }

    [Fact]
    public async Task ReasonForFailure_is_not_empty_when_download_fails()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var sut = fixture.Create<DownloadJob>();
        using var httpClient =
            new HttpClient(
                NewFailingHttpMessageHandler(
                    link.Url,
                    new Exception("Something went wrong!")));
        sut.Start(httpClient);

        await sut.DownloadTask.AwaitIgnoringExceptions();

        sut.ReasonForFailure.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Download_is_saved_to_temporary_file()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var temporaryFileStreamMock = fixture.Create<FileSystemStream>();
        fixture
            .Freeze<IFileSystem>()
            .FileStream.New(Arg.Any<string>(), FileMode.CreateNew)
            .Returns(temporaryFileStreamMock);
        var sut = fixture.Create<DownloadJob>();
        const string content = "file contents";
        using var httpClient =
            new HttpClient(
                NewDefaultHttpMessageHandler(
                    link.Url,
                    new HttpResponseMessage
                    {
                        Content = new StringContent(content)
                    }));
        sut.Start(httpClient);

        await sut.DownloadTask;

        await temporaryFileStreamMock.Received().WriteAsync(
            Arg.Is<ReadOnlyMemory<byte>>(b =>
                b.ToArray().SequenceEqual(Encoding.Default.GetBytes(content))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TemporaryFile_is_moved_to_saveAsFile()
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var saveAsFile = fixture.Freeze<SaveAsFile>();
        string? temporaryFilePath = default;
        var fileSystemMock = fixture.Freeze<IFileSystem>();
        fileSystemMock
            .FileStream
            .New(Arg.Any<string>(), FileMode.CreateNew)
            .Returns(fixture.Create<FileSystemStream>())
            .AndDoes(ci => temporaryFilePath = ci.Arg<string>());
        var sut = fixture.Create<DownloadJob>();
        using var httpClient =
            new HttpClient(
                NewDefaultHttpMessageHandler(
                    link.Url));
        sut.Start(httpClient);

        await sut.DownloadTask;

        temporaryFilePath.Should().NotBeNull();
        fileSystemMock.File
            .Received()
            .Move(
                temporaryFilePath!,
                saveAsFile.FullName,
                false);
    }

    [Theory]
    [InlineData(HttpStatusCode.Moved)]
    [InlineData(HttpStatusCode.Redirect)]
    [InlineData(HttpStatusCode.TemporaryRedirect)]
    [InlineData(HttpStatusCode.PermanentRedirect)]
    public async Task Downloads_when_Link_responds_with_redirect(
        HttpStatusCode redirectStatusCode)
    {
        var fixture = NewFixture();
        var link = fixture.Freeze<Link>();
        var temporaryFileStreamMock = fixture.Create<FileSystemStream>();
        fixture
            .Freeze<IFileSystem>()
            .FileStream.New(Arg.Any<string>(), FileMode.CreateNew)
            .Returns(temporaryFileStreamMock);
        var sut = fixture.Create<DownloadJob>();
        var httpMessageHandlerStub = fixture.Create<HttpMessageHandler>();
        httpMessageHandlerStub
            .GetType()
            .GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(
                httpMessageHandlerStub,
                new object?[]
                {
                    Arg.Is<HttpRequestMessage>(r => r.RequestUri!.OriginalString == link.Url),
                    Arg.Any<CancellationToken>()
                })
            .Returns(Task.FromResult(new HttpResponseMessage
            {
                StatusCode = redirectStatusCode,
                Headers = { Location = new Uri("https://new.address.com/file.iso") }
            }));
        httpMessageHandlerStub
            .GetType()
            .GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(
                httpMessageHandlerStub,
                new object?[]
                {
                    Arg.Is<HttpRequestMessage>(r => r.RequestUri!.OriginalString == "https://new.address.com/file.iso"),
                    Arg.Any<CancellationToken>()
                })
            .Returns(Task.FromResult(new HttpResponseMessage
            {
                Content = new StringContent("file contents")
            }));
        using var httpClient = new HttpClient(httpMessageHandlerStub);
        sut.Start(httpClient);

        await sut.DownloadTask;

        await temporaryFileStreamMock
            .Received()
            .WriteAsync(
                Arg.Is<ReadOnlyMemory<byte>>(b =>
                    b.ToArray().SequenceEqual(Encoding.Default.GetBytes("file contents"))),
                Arg.Any<CancellationToken>());
    }

    private static HttpMessageHandler NewDefaultHttpMessageHandler(
        string requestUrl,
        HttpResponseMessage? response = default)
    {
        var httpMessageHandlerStub = Substitute.For<HttpMessageHandler>();
        httpMessageHandlerStub
            .GetType()
            .GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(
                httpMessageHandlerStub,
                new object?[]
                {
                    Arg.Is<HttpRequestMessage>(r =>
                        r.RequestUri!.OriginalString == requestUrl),
                    Arg.Any<CancellationToken>()
                })
            .Returns(Task.FromResult(response ?? new HttpResponseMessage()));
        return httpMessageHandlerStub;
    }

    private static HttpMessageHandler NewForeverRunningHttpMessageHandler(
        string requestUrl)
    {
        var httpMessageHandlerStub = Substitute.For<HttpMessageHandler>();
        httpMessageHandlerStub
            .GetType()
            .GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(
                httpMessageHandlerStub,
                new object?[]
                {
                    Arg.Is<HttpRequestMessage>(r =>
                        r.RequestUri!.OriginalString == requestUrl),
                    Arg.Any<CancellationToken>()
                })
            .Returns(Task.Run<HttpResponseMessage>(async () =>
            {
                const int forever = -1;
                await Task.Delay(forever);
                throw new InvalidOperationException("Unreachable code");
            }));
        return httpMessageHandlerStub;
    }

    private static HttpMessageHandler NewFailingHttpMessageHandler(
        string requestUrl,
        Exception? exception = default)
    {
        var httpMessageHandlerStub = Substitute.For<HttpMessageHandler>();
        httpMessageHandlerStub
            .GetType()
            .GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(
                httpMessageHandlerStub,
                new object?[]
                {
                    Arg.Is<HttpRequestMessage>(r =>
                        r.RequestUri!.OriginalString == requestUrl),
                    Arg.Any<CancellationToken>()
                })
            .Returns(Task.FromException<HttpResponseMessage>(exception ?? new Exception()));
        return httpMessageHandlerStub;
    }
}