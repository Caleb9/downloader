using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;

namespace Api.Notifications;

[UsedImplicitly]
public sealed class NotificationsHub :
    Hub<NotificationsHub.IClient>
{
    public interface IClient
    {
        [HubMethodName("receiveTotalBytes")]
        Task SendTotalBytes(
            TotalBytesMessage message);

        [HubMethodName("receiveProgress")]
        Task SendProgress(
            IEnumerable<ProgressMessage> message,
            CancellationToken cancellationToken);

        [HubMethodName("receiveFinished")]
        Task SendFinished(
            FinishedMessage message);

        [HubMethodName("receiveFailed")]
        Task SendFailed(
            FailedMessage message);
    }

    public record TotalBytesMessage(string Id, long TotalBytes);

    public record ProgressMessage(string Id, long BytesDownloaded);

    public record FinishedMessage(string Id, string FileName);

    public record FailedMessage(string Id, string Reason);
}