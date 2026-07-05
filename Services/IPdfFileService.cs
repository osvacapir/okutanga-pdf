using OkutangaPDF.Models;

namespace OkutangaPDF.Services;

public interface IPdfFileService
{
    long MaxFileSizeBytes { get; }

    Task<PdfDocumentInfo?> PickAndImportAsync(CancellationToken cancellationToken = default);

    Task<PdfDocumentInfo?> ImportStreamAsync(Stream source, string fileName, CancellationToken cancellationToken = default);

    Task<byte[]?> ReadBytesAsync(PdfDocumentInfo document, CancellationToken cancellationToken = default);

    /// <summary>Abre o PDF no disco sem carregar tudo em memória (para o leitor).</summary>
    Stream? OpenRead(PdfDocumentInfo document);

    Task ShareAsync(PdfDocumentInfo document, CancellationToken cancellationToken = default);

    string FormatFileSize(long bytes);
}
