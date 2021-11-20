using System.Net;
using System.Net.Http.Json;
using Api.Controllers;
using Api.Downloading;
using AutoFixture;
using FluentAssertions;
using TestHelpers;
using Tests.Integration.Extensions;
using Xunit;

namespace Tests.Integration;

public sealed class GetTests :
    IClassFixture<IntegrationTestWebApplicationFactory>
{
    private const string ApiDownloadRoute = "/api/download";
    private readonly IntegrationTestWebApplicationFactory _factory;

    public GetTests(
        IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_returns_contents_of_DownloadJobsDictionary()
    {
        var fixture = new Fixture().Customize(new DownloaderCustomization());
        var job1 = fixture.Create<DownloadJob>();
        var job2 = fixture.Create<DownloadJob>();
        var jobs = new DownloadJobsDictionary { [job1.Id] = job1, [job2.Id] = job2 };

        using var client =
            _factory
                .WithServices(services =>
                    services.ReplaceAllWithSingleton(jobs))
                .CreateDefaultClient();

        using var response = await client.GetAsync(ApiDownloadRoute);

        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadFromJsonAsync<DownloadController.GetResponseDto[]>();
        content.Should()
            .NotBeNullOrEmpty()
            .And.Contain(
                new DownloadController.GetResponseDto(
                    job1.Id,
                    job1.Link,
                    job1.SaveAsFile.Name,
                    job1.Status.ToString(),
                    job1.CreatedTicks))
            .And.Contain(
                new DownloadController.GetResponseDto(
                    job2.Id,
                    job2.Link,
                    job2.SaveAsFile.Name,
                    job2.Status.ToString(),
                    job2.CreatedTicks))
            .And.Subject.Count().Should().Be(2);
    }

    [Fact]
    public async Task Get_by_id_returns_job_when_it_exists()
    {
        var fixture = new Fixture().Customize(new DownloaderCustomization());
        var job = fixture.Create<DownloadJob>();
        var jobs = new DownloadJobsDictionary { [job.Id] = job };

        using var client =
            _factory
                .WithServices(services =>
                    services.ReplaceAllWithSingleton(jobs))
                .CreateDefaultClient();

        using var response = await client.GetAsync($"{ApiDownloadRoute}/{job.Id}");

        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadFromJsonAsync<DownloadController.GetResponseDto>();
        content.Should()
            .NotBeNull()
            .And.Be(
                new DownloadController.GetResponseDto(
                    job.Id,
                    job.Link,
                    job.SaveAsFile.Name,
                    job.Status.ToString(),
                    job.CreatedTicks,
                    job.TotalBytes,
                    job.BytesDownloaded));
    }

    [Fact]
    public async Task Get_by_id_returns_NotFound_when_job_does_not_exist()
    {
        using var client = _factory.CreateDefaultClient();

        using var response = await client.GetAsync($"{ApiDownloadRoute}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}