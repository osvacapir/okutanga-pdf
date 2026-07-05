using OkutangaPDF.Models;

namespace OkutangaPDF.Services;

public interface IRecentDocumentsStore
{
    Task EnsureReadyAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PdfDocumentInfo>> GetRecentAsync(int limit = 50, CancellationToken cancellationToken = default);

    Task<PdfDocumentInfo?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PdfDocumentInfo> UpsertAsync(PdfDocumentInfo document, CancellationToken cancellationToken = default);

    Task UpdateLastPageAsync(int id, int lastPage, int pageCount = 0, double lastZoom = 0, CancellationToken cancellationToken = default);

    Task UpdateLastZoomAsync(int id, double lastZoom, CancellationToken cancellationToken = default);

    Task RemoveAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
