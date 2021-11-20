namespace Tests.Integration;

internal interface IProtectedDelegatingHandler
{
    Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken);
}