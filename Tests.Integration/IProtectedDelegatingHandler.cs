using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Integration
{
    internal interface IProtectedDelegatingHandler
    {
        Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken);
    }
}