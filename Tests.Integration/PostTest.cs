using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Api;
using Api.Controllers;
using Api.Downloading;
using Api.Notifications;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Moq.Protected;
using TestHelpers;
using Tests.Integration.Extensions;
using Xunit;

namespace Tests.Integration
{
    public sealed class PostTest :
        IClassFixture<IntegrationTestWebApplicationFactory>
    {
        private const string ApiDownloadRoute = "/api/download";
        private readonly WebApplicationFactory<Startup> _factory;

        public PostTest(
            IntegrationTestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        ///     This enormous test spans over entire application functionality.
        /// </summary>
        [Fact]
        public async Task Post_starts_download_and_saves_to_file_and_sends_notifications()
        {
            /* Arrange */
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var downloadHttpClientStub = CreateAndSetupDownloadHttpClientStub(fixture);
            var newDownloadId = fixture.Create<DownloadJob.JobId>();
            var fileSystemMock = CreateAndSetupFileSystemMock(fixture, newDownloadId);
            var downloadJobsDictionary = new DownloadJobsDictionary();
            var notificationsHubContextMock =
                fixture.Create<Mock<IHubContext<NotificationsHub, NotificationsHub.IClient>>>();
            using var client =
                CreateClient(
                    newDownloadId,
                    downloadJobsDictionary,
                    fileSystemMock.Object,
                    downloadHttpClientStub.Object,
                    notificationsHubContextMock.Object);

            /* Act */
            var response =
                await client.PostAsync(
                    ApiDownloadRoute,
                    JsonContent.Create(
                        new DownloadController.PostRequestDto(
                            "https://download.stuff/file.iso",
                            "saveAsFile.iso")));

            /* Assert */
            response.IsSuccessStatusCode.Should().BeTrue();
            await ResponseShouldContainNewDownloadId();
            downloadJobsDictionary[newDownloadId].CreatedTicks.Should().Be(42);
            await DownloadContentsShouldBeSavedToTemporaryFile();
            TemporaryFileShouldBeMovedToSaveAsFile();
            SignalRMessagesShouldBeSent();

            async Task ResponseShouldContainNewDownloadId()
            {
                var responseContent = await response.Content.ReadFromJsonAsync<Guid>();
                responseContent.Should().Be(newDownloadId.Value);
            }

            async Task DownloadContentsShouldBeSavedToTemporaryFile()
            {
                var newDownload = downloadJobsDictionary[newDownloadId];
                /* To observe the results we need to simulate completion of the downloading task */
                await newDownload.DownloadTask;
                fileSystemMock.Verify(fs =>
                    fs.FileStream
                        .Create($"/incomplete/{newDownloadId}", FileMode.CreateNew)
                        .WriteAsync(
                            It.Is<ReadOnlyMemory<byte>>(b =>
                                Encoding.Default.GetString(b.ToArray()).Equals("file contents")),
                            It.IsAny<CancellationToken>()));
            }

            void TemporaryFileShouldBeMovedToSaveAsFile()
            {
                fileSystemMock.Verify(fs =>
                    fs.File.Move(
                        $"/incomplete/{newDownloadId}",
                        /* Test that name of the file has incremented index because saveAsFile.iso already exists */
                        "/completed/saveAsFile(1).iso",
                        false));
            }

            void SignalRMessagesShouldBeSent()
            {
                notificationsHubContextMock.Verify(h =>
                    h.Clients.All.SendTotalBytes(
                        It.Is<NotificationsHub.TotalBytesMessage>(m =>
                            m.Id == newDownloadId &&
                            m.TotalBytes == 42)));
                notificationsHubContextMock.Verify(h =>
                    h.Clients.All.SendFinished(
                        It.Is<NotificationsHub.FinishedMessage>(m =>
                            m.Id == newDownloadId &&
                            m.FileName == "saveAsFile(1).iso")));
                notificationsHubContextMock.Verify(h =>
                    h.Clients.All.SendProgress(
                        It.Is<IEnumerable<NotificationsHub.ProgressMessage>>(m =>
                            m.Any(p => p.Id == newDownloadId)),
                        It.IsAny<CancellationToken>()));
            }
        }

        [Fact]
        public async Task Post_sends_failure_notification_if_download_task_fails()
        {
            /* Arrange */
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var newDownloadId = fixture.Create<DownloadJob.JobId>();
            var downloadJobsDictionary = new DownloadJobsDictionary();
            var fileSystemStub = CreateAndSetupFileSystemMock(fixture, newDownloadId);
            var notificationsHubContextMock =
                fixture.Create<Mock<IHubContext<NotificationsHub, NotificationsHub.IClient>>>();
            var downloadHttpClientStub = fixture.Create<Mock<DelegatingHandler>>();
            downloadHttpClientStub
                .Protected()
                .As<IProtectedDelegatingHandler>()
                .Setup(h =>
                    h.SendAsync(
                        It.Is<HttpRequestMessage>(r =>
                            r.Method == HttpMethod.Get &&
                            r.RequestUri!.OriginalString == "https://download.stuff/file.iso"),
                        It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Something went wrong!"));
            using var client =
                CreateClient(
                    newDownloadId,
                    downloadJobsDictionary,
                    fileSystemStub.Object,
                    downloadHttpClientStub.Object,
                    notificationsHubContextMock.Object);
            
            /* Act */
            var response =
                await client.PostAsync(
                    ApiDownloadRoute,
                    JsonContent.Create(
                        new DownloadController.PostRequestDto(
                            "https://download.stuff/file.iso",
                            "saveAsFile.iso")));
            
            /* Assert */
            response.IsSuccessStatusCode.Should().BeTrue();
            var newDownload = downloadJobsDictionary[newDownloadId];
            /* To observe the results we need to simulate completion of the downloading task */
            await newDownload.DownloadTask.AwaitIgnoringExceptions();
            
            notificationsHubContextMock.Verify(h =>
                h.Clients.All.SendFailed(
                    It.Is<NotificationsHub.FailedMessage>(m =>
                        m.Id == newDownloadId &&
                        m.Reason == "Something went wrong!")));
        }

        private static Mock<DelegatingHandler> CreateAndSetupDownloadHttpClientStub(
            ISpecimenBuilder fixture)
        {
            var downloadResponse =
                new HttpResponseMessage
                {
                    Content = new StringContent("file contents")
                };
            downloadResponse.Content.Headers.Add("Content-Length", 42.ToString());
            var httpClientStub = fixture.Create<Mock<DelegatingHandler>>();
            httpClientStub
                .Protected()
                .As<IProtectedDelegatingHandler>()
                .Setup(h =>
                    h.SendAsync(
                        It.Is<HttpRequestMessage>(r =>
                            r.Method == HttpMethod.Get &&
                            r.RequestUri!.OriginalString == "https://download.stuff/file.iso"),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadResponse);
            return httpClientStub;
        }

        private static Mock<IFileSystem> CreateAndSetupFileSystemMock(
            ISpecimenBuilder fixture,
            DownloadJob.JobId newDownloadId)
        {
            var fileSystemMock = fixture.Create<Mock<IFileSystem>>();
            fileSystemMock
                .SetupGet(fs =>
                    fs.FileStream.Create($"/incomplete/{newDownloadId}", FileMode.CreateNew).CanWrite)
                .Returns(true);
            fileSystemMock
                /* Simulate saveAsFile.iso already existing to test name incrementing logic */
                .Setup(fs =>
                    fs.File.Move(
                        $"/incomplete/{newDownloadId}",
                        "/completed/saveAsFile.iso",
                        false))
                .Throws<IOException>();
            return fileSystemMock;
        }

        private HttpClient CreateClient(
            DownloadJob.JobId newDownloadId,
            DownloadJobsDictionary downloadJobsDictionary,
            IFileSystem fileSystem,
            DelegatingHandler downloadsHttpClient,
            IHubContext<NotificationsHub, NotificationsHub.IClient> notificationsHubContext)
        {
            return
                _factory
                    .WithSettings(
                        ("DownloadDirectories:Incomplete", "/incomplete"),
                        ("DownloadDirectories:Completed", "/completed"),
                        /* One millisecond interval to give ProgressNotificationsBackgroundService a chance to iterate
                         * through dictionary while the test is executing - this can potentially cause a flaky test.
                         * See SignalRMessagesShouldBeSent() verification for SendProgress in the Assert block below. */
                        ("PushNotifications:ProgressIntervalInMilliseconds", "1"))
                    .WithServices(services =>
                        services
                            .ReplaceAllWithSingleton(new DownloadManager.DownloadIdGenerator(() => newDownloadId))
                            .ReplaceAllWithSingleton(new DownloadManager.DateTimeUtcNowTicks(() => 42))
                            .ReplaceAllWithSingleton(downloadJobsDictionary)
                            .ReplaceAllWithSingleton(fileSystem)
                            .ReplaceAllWithSingleton(notificationsHubContext)
                            .ReplaceHttpMessageHandlerFor<DownloadStarter>(downloadsHttpClient))
                    .CreateDefaultClient();
        }
    }
}