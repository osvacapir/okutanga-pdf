namespace OlondongeApp.Services;

public sealed class ConnectivityChangedInfo : EventArgs
{
    public bool IsOnline { get; init; }
}

