using OkutangaPDF.Models;

namespace OkutangaPDF.Services;

public interface IPdfReaderSession
{
    PdfDocumentInfo? Current { get; }

    event Action? DocumentChanged;

    Task<PdfDocumentInfo?> OpenPickerAsync(CancellationToken cancellationToken = default);

    Task<PdfDocumentInfo?> OpenRecentAsync(int documentId, CancellationToken cancellationToken = default);

    void SetCurrent(PdfDocumentInfo? document);

    Task SaveReadingProgressAsync(int lastPage, int pageCount = 0, double lastZoom = 0, CancellationToken cancellationToken = default);
}
