using System.ComponentModel.DataAnnotations;
using Api.Downloading;
using Api.Downloading.Directories;
using Api.Notifications;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DownloadController(
        DownloadManager downloadManager,
        NotificationsManager notificationsManager,
        DownloadStarter downloadStarter,
        CompletedDownloadsDirectory completedDownloadsDirectory,
        DownloadJobsDictionary jobs)
    : ControllerBase
{
    [HttpPost]
    public ActionResult<Guid> Post(
        PostRequestDto dto)
    {
        var (linkString, saveAsFileName) = dto;
        var dtoValidationResult =
            Link.Create(linkString)
                .Bind(l => SaveAsFile.Create(l, completedDownloadsDirectory, saveAsFileName)
                    .Bind(s => Result.Success((link: l, saveAsFile: s))));
        if (dtoValidationResult.IsFailure)
        {
            return BadRequest(dtoValidationResult.Error);
        }

        var (link, saveAsFile) = dtoValidationResult.Value;
        var job =
            notificationsManager.AddNotificationEventHandlers(
                downloadManager.CreateDownloadJob(link, saveAsFile));

        var (_, isFailure, error) = downloadStarter.Start(job);
        return isFailure ? Problem(error) : Ok(job.Id.Value);
    }

    [HttpGet]
    public IEnumerable<GetResponseDto> Get()
    {
        return jobs.Values.Select(AsDto);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<GetResponseDto> Get(
        Guid id)
    {
        return
            jobs.TryGetValue(new DownloadJob.JobId(id), out var download)
                ? AsDto(download)
                : NotFound(id);
    }

    [HttpDelete]
    public void Delete()
    {
        downloadManager.Cleanup();
    }

    private GetResponseDto AsDto(
        DownloadJob downloadJob)
    {
        if (downloadJob.Status is DownloadJob.DownloadStatus.NotStarted)
        {
            return new GetResponseDto(
                downloadJob.Id,
                downloadJob.Link,
                downloadJob.SaveAsFile.Name,
                downloadJob.Status.ToString(),
                downloadJob.CreatedTicks);
        }

        return new GetResponseDto(
            downloadJob.Id,
            downloadJob.Link,
            downloadJob.SaveAsFile.Name,
            downloadJob.Status.ToString(),
            downloadJob.CreatedTicks,
            downloadJob.TotalBytes,
            downloadJob.BytesDownloaded,
            downloadJob.ReasonForFailure);
    }

    public record PostRequestDto(
        [Required] string Link,
        string SaveAsFileName = "");

    public record GetResponseDto(
        Guid Id,
        string Link,
        string SaveAsFile,
        string Status,
        long CreatedTicks,
        long TotalBytes = -1,
        long BytesDownloaded = 0,
        string ReasonForFailure = "");
}