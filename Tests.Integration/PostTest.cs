using System.IO.Abstractions;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using Api;
using Api.Controllers;
using Api.Downloading;
using Api.Notifications;
using AutoFixture;
using AutoFixture.Kernel;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using TestHelpers;
using Tests.Integration.Extensions;
using Xunit;

namespace Tests.Integration;

public sealed class PostTest(
        IntegrationTestWebApplicationFactory factory)
    : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private const string ApiDownloadRoute = "/api/download";
    private readonly WebApplicationFactory<Startup> _factory = factory;

    /// <summary>
    ///     This enormous test spans over entire application functionality.
    /// </summary>
    [Fact]
    public async Task Post_starts_download_and_saves_to_file_and_sends_notifications()
    {
        /* Arrange */
        var fixture = new Fixture().Customize(new DownloaderCustomization());
        var downloadHttpClientStub = CreateAndSetupDownloadHttpClientStub(fixture);
        var newDownloadId = fixture.Create<DownloadJob.JobId>();
        var fileSystemMock = CreateAndSetupFileSystemMock(fixture, newDownloadId);
        var downloadJobsDictionary = new DownloadJobsDictionary();
        var notificationsHubContextMock =
            fixture.Create<IHubContext<NotificationsHub, NotificationsHub.IClient>>();
        using var client =
            CreateClient(
                newDownloadId,
                downloadJobsDictionary,
                fileSystemMock,
                downloadHttpClientStub,
                notificationsHubContextMock);

        /* Act */
        using var response =
            await client.PostAsync(
                ApiDownloadRoute,
                JsonContent.Create(
                    new DownloadController.PostRequestDto(
                        "https://download.stuff/file.iso",
                        "saveAsFile.iso")));

        /* Assert */
        response.IsSuccessStatusCode.Should().BeTrue();
        await ResponseShouldContainNewDownloadId(response);
        downloadJobsDictionary[newDownloadId].CreatedTicks.Should().Be(42);
        await DownloadContentsShouldBeSavedToTemporaryFile();
        TemporaryFileShouldBeMovedToSaveAsFile();
        SignalRMessagesShouldBeSent();

        async Task ResponseShouldContainNewDownloadId(HttpResponseMessage r)
        {
            var responseContent = await r.Content.ReadFromJsonAsync<Guid>();
            responseContent.Should().Be(newDownloadId.Value);
        }

        async Task DownloadContentsShouldBeSavedToTemporaryFile()
        {
            var newDownload = downloadJobsDictionary[newDownloadId];
            await newDownload.DownloadTask;
            fileSystemMock
                .FileStream
                .Received()
                .New($"/incomplete/{newDownloadId}", FileMode.CreateNew);
            /* To observe the results we need to simulate completion of the downloading task.
             * Fixture has been set up to always return the same instance of FileSystemStream, so to verify the calls
             * we need to first obtain that instance. Note: "file contents" have been set up in
             * CreateAndSetupDownloadHttpClientStub. */
            await fixture.Create<FileSystemStream>()
                .Received()
                .WriteAsync(
                    Arg.Is<ReadOnlyMemory<byte>>(b =>
                        Encoding.Default.GetString(b.ToArray()).Equals("file contents")),
                    Arg.Any<CancellationToken>());
        }

        void TemporaryFileShouldBeMovedToSaveAsFile()
        {
            fileSystemMock.File.Received().Move(
                $"/incomplete/{newDownloadId}",
                /* Test that name of the file has incremented index because saveAsFile.iso already exists */
                "/completed/saveAsFile(1).iso",
                false);
        }

        void SignalRMessagesShouldBeSent()
        {
            notificationsHubContextMock.Clients.All.Received().SendTotalBytes(
                Arg.Is<NotificationsHub.TotalBytesMessage>(m =>
                    m.Id == newDownloadId &&
                    m.TotalBytes == 42));
            notificationsHubContextMock.Clients.All.Received().SendFinished(
                Arg.Is<NotificationsHub.FinishedMessage>(m =>
                    m.Id == newDownloadId &&
                    m.FileName == "saveAsFile(1).iso"));
            notificationsHubContextMock.Clients.All.Received().SendProgress(
                Arg.Is<IEnumerable<NotificationsHub.ProgressMessage>>(m =>
                    m.Any(p => p.Id == newDownloadId)),
                Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Post_sends_failure_notification_if_download_task_fails()
    {
        /* Arrange */
        var fixture = new Fixture().Customize(new DownloaderCustomization());
        var newDownloadId = fixture.Create<DownloadJob.JobId>();
        var downloadJobsDictionary = new DownloadJobsDictionary();
        var fileSystemStub = CreateAndSetupFileSystemMock(fixture, newDownloadId);
        var notificationsHubContextMock =
            fixture.Create<IHubContext<NotificationsHub, NotificationsHub.IClient>>();
        var downloadHttpClientStub = fixture.Create<DelegatingHandler>();
        downloadHttpClientStub
            .GetType()
            .GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(downloadHttpClientStub,
                new object?[]
                {
                    Arg.Is<HttpRequestMessage>(r =>
                        r.Method == HttpMethod.Get &&
                        r.RequestUri!.OriginalString == "https://download.stuff/file.iso"),
                    Arg.Any<CancellationToken>()
                })
            .Returns(Task.FromException<HttpResponseMessage>(new Exception("Something went wrong!")));
        using var client =
            CreateClient(
                newDownloadId,
                downloadJobsDictionary,
                fileSystemStub,
                downloadHttpClientStub,
                notificationsHubContextMock);

        /* Act */
        using var response =
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

        await notificationsHubContextMock.Clients.All.Received().SendFailed(
            Arg.Is<NotificationsHub.FailedMessage>(m =>
                m.Id == newDownloadId &&
                m.Reason == "Something went wrong!"));
    }

    private static DelegatingHandler CreateAndSetupDownloadHttpClientStub(
        ISpecimenBuilder fixture)
    {
        var downloadResponse =
            new HttpResponseMessage
            {
                Content = new StringContent("file contents")
            };
        downloadResponse.Content.Headers.Add("Content-Length", 42.ToString());
        var httpClientStub = fixture.Create<DelegatingHandler>();
        httpClientStub
            .GetType()
            .GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(httpClientStub,
                new object?[]
                {
                    Arg.Is<HttpRequestMessage>(r =>
                        r.Method == HttpMethod.Get &&
                        r.RequestUri!.OriginalString == "https://download.stuff/file.iso"),
                    Arg.Any<CancellationToken>()
                })
            .Returns(Task.FromResult(downloadResponse));
        return httpClientStub;
    }

    private static IFileSystem CreateAndSetupFileSystemMock(
        ISpecimenBuilder fixture,
        DownloadJob.JobId newDownloadId)
    {
        var fileSystemMock = fixture.Create<IFileSystem>();
        fileSystemMock
            /* Simulate saveAsFile.iso already existing to test name incrementing logic */
            .File.When(f => f.Move(
                $"/incomplete/{newDownloadId}",
                "/completed/saveAsFile.iso",
                false))
            .Throw<IOException>();
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