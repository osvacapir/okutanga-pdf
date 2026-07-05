using OkutangaPDF.Models;

namespace OkutangaPDF.Services;

public sealed class PdfReaderSession : IPdfReaderSession
{
    private readonly IPdfFileService _fileService;
    private readonly IRecentDocumentsStore _recentStore;

    public PdfReaderSession(IPdfFileService fileService, IRecentDocumentsStore recentStore)
    {
        _fileService = fileService;
        _recentStore = recentStore;
    }

    public PdfDocumentInfo? Current { get; private set; }

    public event Action? DocumentChanged;

    public async Task<PdfDocumentInfo?> OpenPickerAsync(CancellationToken cancellationToken = default)
    {
        var doc = await _fileService.PickAndImportAsync(cancellationToken);
        SetCurrent(doc);
        return doc;
    }

    public async Task<PdfDocumentInfo?> OpenRecentAsync(int documentId, CancellationToken cancellationToken = default)
    {
        var doc = await _recentStore.GetByIdAsync(documentId, cancellationToken);
        if (doc is null || !doc.FileExists)
        {
            if (doc is not null)
            {
                await _recentStore.RemoveAsync(doc.Id, cancellationToken);
            }

            return null;
        }

        doc = await _recentStore.UpsertAsync(doc, cancellationToken);
        SetCurrent(doc);
        return doc;
    }

    public void SetCurrent(PdfDocumentInfo? document)
    {
        Current = document;
        DocumentChanged?.Invoke();
    }

    public async Task SaveReadingProgressAsync(int lastPage, int pageCount = 0, double lastZoom = 0, CancellationToken cancellationToken = default)
    {
        if (Current is null)
        {
            return;
        }

        Current.LastPage = Math.Max(1, lastPage);
        if (pageCount > 0)
        {
            Current.PageCount = pageCount;
        }

        if (lastZoom > 0)
        {
            Current.LastZoom = lastZoom;
        }

        await _recentStore.UpdateLastPageAsync(Current.Id, Current.LastPage, pageCount, lastZoom, cancellationToken);
    }
}
