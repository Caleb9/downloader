using System.ComponentModel.DataAnnotations;

namespace Api.Dto
{
    public record PostRequestDto(
        [Required] string Link,
        string SaveAsFileName = "");
}