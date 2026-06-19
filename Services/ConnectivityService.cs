using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Networking;

namespace OlondongeApp.Services;

public sealed class ConnectivityService : IConnectivityService
{
    private bool _disposed;

    public ConnectivityService()
    {
        Connectivity.ConnectivityChanged += OnMauiConnectivityChanged;
    }

    public bool IsOnline => Connectivity.NetworkAccess == NetworkAccess.Internet;

    public Task<bool> IsOnlineAsync() => Task.FromResult(IsOnline);

    public event EventHandler<ConnectivityChangedInfo>? ReachabilityChanged;

    private void OnMauiConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var online = e.NetworkAccess == NetworkAccess.Internet;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ReachabilityChanged?.Invoke(this, new ConnectivityChangedInfo { IsOnline = online });
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Connectivity.ConnectivityChanged -= OnMauiConnectivityChanged;
    }
}

