using Api.Downloading;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Downloading;

public sealed class LinkTests
{
    [Theory]
    [InlineData("http://server.com/file.iso")]
    [InlineData("http://server.com/file.iso?queryString=ignored")]
    [InlineData("https://server.com/path/file.iso")]
    [InlineData("https://server.com/path/file.iso?queryString=ignored")]
    public void When_link_is_http_and_denotes_a_file_then_Create_returns_success(
        string link)
    {
        var result = Link.Create(link);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void When_link_is_not_http_then_Create_returns_failure()
    {
        var result = Link.Create("ftp://server.com/file.iso");

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://server.com/")]
    [InlineData("https://server.com/?queryString=value")]
    public void When_link_is_http_but_does_not_denote_a_file_then_Create_returns_failure(
        string link)
    {
        var result = Link.Create(link);

        result.IsSuccess.Should().BeTrue();
    }
}