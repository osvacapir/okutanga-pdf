namespace OkutangaPDF.Models;

public enum ToastSeverity
{
    Info,
    Success,
    Warning,
    Error,
}

public sealed class ToastMessage
{
    public ToastSeverity Severity { get; init; } = ToastSeverity.Info;

    public string? Summary { get; init; }

    public string? Detail { get; init; }

    public int Duration { get; init; } = 4000;
}
