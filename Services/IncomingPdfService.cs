using OkutangaPDF.Models;

namespace OkutangaPDF.Services;

public interface IIncomingPdfService
{
    bool HasPending { get; }

    event Action? PendingAvailable;

    Task EnqueueFromStreamAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);

    Task EnqueueFromPathAsync(string path, CancellationToken cancellationToken = default);

    Task<PdfDocumentInfo?> ConsumePendingAsync(CancellationToken cancellationToken = default);
}

public sealed class IncomingPdfService : IIncomingPdfService
{
    private readonly IPdfFileService _fileService;
    private readonly object _lock = new();
    private PdfDocumentInfo? _pending;

    public IncomingPdfService(IPdfFileService fileService)
    {
        _fileService = fileService;
    }

    public bool HasPending
    {
        get
        {
            lock (_lock)
            {
                return _pending is not null;
            }
        }
    }

    public event Action? PendingAvailable;

    public async Task EnqueueFromStreamAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var doc = await _fileService.ImportStreamAsync(stream, fileName, cancellationToken);
        if (doc is null)
        {
            return;
        }

        lock (_lock)
        {
            _pending = doc;
        }

        PendingAvailable?.Invoke();
    }

    public async Task EnqueueFromPathAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return;
        }

        await using var stream = File.OpenRead(path);
        var name = Path.GetFileName(path);
        await EnqueueFromStreamAsync(stream, name, cancellationToken);
    }

    public Task<PdfDocumentInfo?> ConsumePendingAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var doc = _pending;
            _pending = null;
            return Task.FromResult(doc);
        }
    }
}
