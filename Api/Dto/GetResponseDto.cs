using System;
using Api.Downloading;

namespace Api.Dto
{
    public record GetResponseDto(
        Guid Id,
        Download.DownloadStatus Status);
}
