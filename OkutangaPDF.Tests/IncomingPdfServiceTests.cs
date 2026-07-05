using OkutangaPDF.Helpers;
using OkutangaPDF.Models;
using OkutangaPDF.Services;

namespace OkutangaPDF.Tests;

[TestClass]
public sealed class IncomingPdfServiceTests
{
    [TestMethod]
    public async Task EnqueueFromStream_SetsPendingAndConsumeClears()
    {
        var fileService = new FakePdfFileService();
        var service = new IncomingPdfService(fileService);
        var notified = false;
        service.PendingAvailable += () => notified = true;

        await using var stream = new MemoryStream([1, 2, 3]);
        await service.EnqueueFromStreamAsync(stream, "teste.pdf");

        Assert.IsTrue(service.HasPending);
        Assert.IsTrue(notified);

        var consumed = await service.ConsumePendingAsync();
        Assert.IsNotNull(consumed);
        Assert.AreEqual("teste.pdf", consumed!.FileName);
        Assert.IsFalse(service.HasPending);
        Assert.IsNull(await service.ConsumePendingAsync());
    }

    [TestMethod]
    public async Task EnqueueFromPath_IgnoresMissingFile()
    {
        var service = new IncomingPdfService(new FakePdfFileService());
        await service.EnqueueFromPathAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf"));
        Assert.IsFalse(service.HasPending);
    }

    private sealed class FakePdfFileService : IPdfFileService
    {
        public long MaxFileSizeBytes => PdfFileHelpers.DefaultMaxFileSizeBytes;

        public Task<PdfDocumentInfo?> PickAndImportAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<PdfDocumentInfo?>(null);

        public Task<PdfDocumentInfo?> ImportStreamAsync(Stream source, string fileName, CancellationToken cancellationToken = default)
            => ImportStreamAsync(source, fileName, cancellationToken, null);

        public Task<PdfDocumentInfo?> ImportStreamAsync(Stream source, string fileName, CancellationToken cancellationToken, string? destPath)
        {
            return Task.FromResult<PdfDocumentInfo?>(new PdfDocumentInfo
            {
                Id = 1,
                FileName = fileName,
                LocalPath = destPath ?? Path.Combine(Path.GetTempPath(), fileName),
                OpenedAtUtc = DateTime.UtcNow,
                FileSizeBytes = source.CanSeek ? source.Length : 0,
                LastPage = 1,
                LastZoom = 1.0,
            });
        }

        public Task<byte[]?> ReadBytesAsync(PdfDocumentInfo document, CancellationToken cancellationToken = default)
            => Task.FromResult<byte[]?>([]);

        public Stream? OpenRead(PdfDocumentInfo document) => null;

        public Task ShareAsync(PdfDocumentInfo document, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public string FormatFileSize(long bytes) => PdfFileHelpers.FormatFileSize(bytes);
    }
}
