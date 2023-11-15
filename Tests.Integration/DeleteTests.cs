using System.Reflection;
using Api.Downloading;
using AutoFixture;
using FluentAssertions;
using NSubstitute;
using TestHelpers;
using Tests.Integration.Extensions;
using Xunit;

namespace Tests.Integration;

public sealed class DeleteTests(
        IntegrationTestWebApplicationFactory factory)
    : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private const string ApiDownloadRoute = "/api/download";

    /// <summary>
    ///     Sets up a protected SendAsync method on a DelegatingHandler stub.
    /// </summary>
    private static void SetupSendAsync(
        DelegatingHandler stub,
        Task<HttpResponseMessage> returnValue)
    {
        stub
            .GetType()
            .GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(stub,
                new object?[] { Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>() })
            .Returns(returnValue);
    }

    [Fact]
    public async Task Delete_removes_completed_and_failed_jobs()
    {
        /* Arrange */
        var fixture = new Fixture().Customize(new DownloaderCustomization());
        var runningJob = fixture.Create<DownloadJob>();
        var downloadingHandlerStub = fixture.Create<DelegatingHandler>();
        SetupSendAsync(
            downloadingHandlerStub,
            Task.Run<HttpResponseMessage>(async () =>
            {
                const int forever = -1;
                await Task.Delay(forever);
                throw new InvalidOperationException("Unreachable code");
            }));
        using var httpClient1 = new HttpClient(downloadingHandlerStub);
        runningJob.Start(httpClient1);
        var completedJob = fixture.Create<DownloadJob>();
        var completedHandlerStub = fixture.Create<DelegatingHandler>();
        SetupSendAsync(
            completedHandlerStub,
            Task.FromResult(new HttpResponseMessage()));
        using var completedJobHttpClient = new HttpClient(completedHandlerStub);
        completedJob.Start(completedJobHttpClient);
        await completedJob.DownloadTask;
        var failedJob = fixture.Create<DownloadJob>();
        var failedHandlerStub = fixture.Create<DelegatingHandler>();
        SetupSendAsync(
            failedHandlerStub,
            Task.FromException<HttpResponseMessage>(new Exception()));
        using var failedJobHttpClient = new HttpClient(failedHandlerStub);
        failedJob.Start(failedJobHttpClient);
        await failedJob.DownloadTask.AwaitIgnoringExceptions();

        var jobs = new DownloadJobsDictionary { [runningJob.Id] = runningJob, [completedJob.Id] = completedJob };

        using var client =
            factory
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