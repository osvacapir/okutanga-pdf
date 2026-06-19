namespace OlondongeApp.Services;

public interface IGradesUpdateNotifier
{
    Task EnsureNotificationPermissionAsync(CancellationToken cancellationToken = default);

    Task NotifyGradesUpdatedAsync(CancellationToken cancellationToken = default);
}
