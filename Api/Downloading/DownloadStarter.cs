using CSharpFunctionalExtensions;

namespace Api.Downloading;

/// <summary>
///     The sole purpose for this class to exist is to be able to configure DI container with HttpClient which
///     can be over-registered (replaced) in integration tests. Injecting HttpClient directly into a Controller
///     makes it impossible to swap the client in tests, probably because of different lifestyles for the client
///     and the controller.
/// </summary>
public sealed class DownloadStarter(
    HttpClient httpClient)
{
    internal Result Start(
        DownloadJob job)
    {
        return job.Start(httpClient);
    }
}