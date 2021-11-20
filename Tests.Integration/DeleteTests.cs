using Api.Downloading;
using AutoFixture;
using FluentAssertions;
using Moq;
using Moq.Protected;
using TestHelpers;
using Tests.Integration.Extensions;
using Xunit;

namespace Tests.Integration;

public sealed class DeleteTests :
    IClassFixture<IntegrationTestWebApplicationFactory>
{
    private const string ApiDownloadRoute = "/api/download";
    private readonly IntegrationTestWebApplicationFactory _factory;

    public DeleteTests(
        IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_removes_completed_and_failed_jobs()
    {
        /* Arrange */
        var fixture = new Fixture().Customize(new DownloaderCustomization());
        var runningJob = fixture.Create<DownloadJob>();
        var downloadingHandlerStub = fixture.Create<Mock<DelegatingHandler>>();
        downloadingHandlerStub
            .Protected()
            .As<IProtectedDelegatingHandler>()
            .Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                const int forever = -1;
                await Task.Delay(forever);
                throw new InvalidOperationException("Unreachable code");
            });
        using var httpClient1 = new HttpClient(downloadingHandlerStub.Object);
        runningJob.Start(httpClient1);
        var completedJob = fixture.Create<DownloadJob>();
        var completedHandlerStub = fixture.Create<Mock<DelegatingHandler>>();
        completedHandlerStub
            .Protected()
            .As<IProtectedDelegatingHandler>()
            .Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage());
        using var completedJobHttpClient = new HttpClient(completedHandlerStub.Object);
        completedJob.Start(completedJobHttpClient);
        await completedJob.DownloadTask;
        var failedJob = fixture.Create<DownloadJob>();
        var failedHandlerStub = fixture.Create<Mock<DelegatingHandler>>();
        failedHandlerStub
            .Protected()
            .As<IProtectedDelegatingHandler>()
            .Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());
        using var failedJobHttpClient = new HttpClient(failedHandlerStub.Object);
        failedJob.Start(failedJobHttpClient);
        await failedJob.DownloadTask.AwaitIgnoringExceptions();

        var jobs = new DownloadJobsDictionary { [runningJob.Id] = runningJob, [completedJob.Id] = completedJob };

        using var client =
            _factory
                .WithServices(services =>
                    services.ReplaceAllWithSingleton(jobs))
                .CreateDefaultClient();

        /* Act */
        using var response = await client.DeleteAsync(ApiDownloadRoute);

        /* Assert */
        response.IsSuccessStatusCode.Should().BeTrue();
        jobs.Should()
            .NotContainKeys(completedJob.Id, failedJob.Id)
            .And.ContainKey(runningJob.Id);
    }
}