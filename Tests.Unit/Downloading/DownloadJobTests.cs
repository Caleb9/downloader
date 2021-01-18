using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Api.Downloading;
using AutoFixture;
using FluentAssertions;
using Moq;
using Moq.Protected;
using TestHelpers;
using Xunit;

namespace Tests.Unit.Downloading
{
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
            var result = sut.Start(Mock.Of<HttpClient>());

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
                            Content = new StreamContent(Mock.Of<Stream>())
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
                            Content = new StreamContent(Mock.Of<Stream>())
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
            var temporaryFileStreamMock = new Mock<Stream>();
            fixture
                .Freeze<Mock<IFileSystem>>()
                .Setup(fs => fs.FileStream.Create(It.IsAny<string>(), FileMode.CreateNew))
                .Returns(temporaryFileStreamMock.Object);
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

            temporaryFileStreamMock.Verify(s =>
                s.WriteAsync(It.Is<ReadOnlyMemory<byte>>(b =>
                    b.ToArray().SequenceEqual(Encoding.Default.GetBytes(content))),
                    It.IsAny<CancellationToken>()));
        }
        
        [Fact]
        public async Task TemporaryFile_is_moved_to_saveAsFile()
        {
            var fixture = NewFixture();
            var link = fixture.Freeze<Link>();
            var saveAsFile = fixture.Freeze<SaveAsFile>();
            string? temporaryFilePath = default;
            var fileSystemMock = fixture.Freeze<Mock<IFileSystem>>();
            fileSystemMock
                .Setup(fs => fs.FileStream.Create(It.IsAny<string>(), FileMode.CreateNew))
                .Callback<string, FileMode>((path, _) => temporaryFilePath = path)
                .Returns(Mock.Of<Stream>());
            var sut = fixture.Create<DownloadJob>();
            using var httpClient =
                new HttpClient(
                    NewDefaultHttpMessageHandler(
                        link.Url));
            sut.Start(httpClient);

            await sut.DownloadTask;

            fileSystemMock.Verify(fs =>
                fs.File.Move(
                    temporaryFilePath,
                    saveAsFile.FullName,
                    false));
        }
        
        private static HttpMessageHandler NewDefaultHttpMessageHandler(
            string requestUrl,
            HttpResponseMessage? response = default)
        {
            var httpMessageHandlerStub = new Mock<HttpMessageHandler>();
            httpMessageHandlerStub
                .Protected().As<IProtectedHttpMessageHandler>()
                .Setup(h =>
                    h.SendAsync(
                        It.Is<HttpRequestMessage>(r =>
                            r.RequestUri!.OriginalString == requestUrl),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(response ?? new HttpResponseMessage());
            return httpMessageHandlerStub.Object;
        }

        private static HttpMessageHandler NewForeverRunningHttpMessageHandler(
            string requestUrl)
        {
            var httpMessageHandlerStub = new Mock<HttpMessageHandler>();
            httpMessageHandlerStub
                .Protected().As<IProtectedHttpMessageHandler>()
                .Setup(h =>
                    h.SendAsync(
                        It.Is<HttpRequestMessage>(r =>
                            r.RequestUri!.OriginalString == requestUrl),
                        It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    const int forever = -1;
                    await Task.Delay(forever);
                    throw new InvalidOperationException("Unreachable code");
                });
            return httpMessageHandlerStub.Object;
        }

        private static HttpMessageHandler NewFailingHttpMessageHandler(
            string requestUrl,
            Exception? exception = default)
        {
            var httpMessageHandlerStub = new Mock<HttpMessageHandler>();
            httpMessageHandlerStub
                .Protected().As<IProtectedHttpMessageHandler>()
                .Setup(h =>
                    h.SendAsync(
                        It.Is<HttpRequestMessage>(r =>
                            r.RequestUri!.OriginalString == requestUrl),
                        It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception ?? new Exception());
            return httpMessageHandlerStub.Object;
        }


        private interface IProtectedHttpMessageHandler
        {
            Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken);
        }
    }
}