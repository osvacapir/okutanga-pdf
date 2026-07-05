using OkutangaPDF.Models;

namespace OkutangaPDF.Services;

public interface IPdfBookmarkStore
{
    Task EnsureReadyAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PdfBookmark>> GetForDocumentAsync(int documentId, CancellationToken cancellationToken = default);

    Task<PdfBookmark> AddAsync(int documentId, int pageNumber, string? label = null, CancellationToken cancellationToken = default);

    Task RemoveAsync(int bookmarkId, CancellationToken cancellationToken = default);
}
