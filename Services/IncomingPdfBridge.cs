namespace OkutangaPDF.Services;

/// <summary>Ponte estática para intents recebidos antes do DI estar pronto.</summary>
public static class IncomingPdfBridge
{
    private static string? _pendingPath;
    private static Stream? _pendingStream;
    private static string? _pendingFileName;

    public static bool HasBridgePending =>
        !string.IsNullOrWhiteSpace(_pendingPath) || _pendingStream is not null;

    public static void SetPendingPath(string path) => _pendingPath = path;

    public static void SetPendingStream(Stream stream, string fileName)
    {
        _pendingStream = stream;
        _pendingFileName = fileName;
    }

    public static async Task FlushToServiceAsync(IIncomingPdfService service, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_pendingPath))
        {
            var path = _pendingPath;
            _pendingPath = null;
            await service.EnqueueFromPathAsync(path!, cancellationToken);
            return;
        }

        if (_pendingStream is not null)
        {
            await using var stream = _pendingStream;
            _pendingStream = null;
            var name = _pendingFileName ?? "documento.pdf";
            _pendingFileName = null;
            await service.EnqueueFromStreamAsync(stream, name, cancellationToken);
        }
    }
}
