using System;
using System.Collections.Generic;
using System.Linq;
using Api.Downloading;
using Api.Downloading.Directories;
using Api.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class DownloadController : ControllerBase
    {
        private readonly CompletedDownloadsDirectory _completedDownloadsDirectory;
        private readonly Downloads _downloads;


        public DownloadController(
            Downloads downloads,
            CompletedDownloadsDirectory completedDownloadsDirectory)
        {
            _downloads = downloads;
            _completedDownloadsDirectory = completedDownloadsDirectory;
        }

        [HttpPost]
        public ActionResult<Guid> Post(
            PostRequestDto dto)
        {
            var (linkString, saveAsFileName) = dto;
            var linkResult = Link.Create(linkString);
            if (linkResult.IsFailure)
            {
                return BadRequest(linkResult.Error);
            }

            var link = linkResult.Value;
            var saveAsFileResult =
                SaveAsFile.Create(
                    link,
                    _completedDownloadsDirectory,
                    saveAsFileName);
            if (saveAsFileResult.IsFailure)
            {
                return BadRequest(saveAsFileResult.Error);
            }

            var taskResult = _downloads.AddAndStart(link, saveAsFileResult.Value);
            if (taskResult.IsFailure)
            {
                return Problem(taskResult.Error);
            }

            return taskResult.Value;
        }

        [HttpGet]
        public IEnumerable<GetResponseDto> Get()
        {
            return _downloads.GetAll().Select(AsDto);
        }

        [HttpGet("{id}")]
        public ActionResult<GetResponseDto> Get(
            Guid id)
        {
            var result = _downloads.Get(id);
            if (result.IsFailure)
            {
                return NotFound(result.Error);
            }

            var download = result.Value;
            return AsDto(download);
        }

        private GetResponseDto AsDto(
            Download download)
        {
            return new(download.Id, download.Status);
        }
    }
}