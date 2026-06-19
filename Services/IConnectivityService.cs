namespace OlondongeApp.Services;

public interface IConnectivityService : IDisposable
{
    bool IsOnline { get; }

    Task<bool> IsOnlineAsync();

    event EventHandler<ConnectivityChangedInfo>? ReachabilityChanged;
}

