using JetBrains.Annotations;

namespace Api.Options;

public sealed class PushNotificationsOptions
{
    internal const string Section = "PushNotifications";

    public int ProgressIntervalInMilliseconds { get; [UsedImplicitly] init; } = 1000;
}