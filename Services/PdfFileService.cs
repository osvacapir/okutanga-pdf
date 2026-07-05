using OkutangaPDF.Helpers;
using OkutangaPDF.Models;

namespace OkutangaPDF.Services;

public sealed class PdfFileService : IPdfFileService
{
    private const long DefaultMaxBytes = PdfFileHelpers.DefaultMaxFileSizeBytes;

    private readonly IRecentDocumentsStore _recentStore;

    public PdfFileService(IRecentDocumentsStore recentStore)
    {
        _recentStore = recentStore;
    }

    public long MaxFileSizeBytes => DefaultMaxBytes;

    public async Task<PdfDocumentInfo?> PickAndImportAsync(CancellationToken cancellationToken = default)
    {
        var pick = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Seleccionar PDF",
            FileTypes = PdfFileTypes,
        });

        if (pick is null)
        {
            return null;
        }

        var fileName = PdfFileHelpers.SanitizeFileName(pick.FileName);
        var docsDir = Path.Combine(FileSystem.AppDataDirectory, "documents");
        Directory.CreateDirectory(docsDir);
        var destPath = Path.Combine(docsDir, $"{Guid.NewGuid():N}_{fileName}");

#if WINDOWS
        var sourcePath = pick.FullPath;
        if (!string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath))
        {
            var sourceInfo = new FileInfo(sourcePath);
            if (sourceInfo.Length > MaxFileSizeBytes)
            {
                throw new InvalidOperationException(
                    $"O ficheiro excede o limite de {FormatFileSize(MaxFileSizeBytes)}.");
            }

            File.Copy(sourcePath, destPath, overwrite: false);
            return await UpsertImportedFileAsync(fileName, destPath, sourceInfo.Length, cancellationToken);
        }
#endif

        await using var source = await pick.OpenReadAsync();
        return await ImportStreamAsync(source, fileName, cancellationToken, destPath);
    }

    public Task<PdfDocumentInfo?> ImportStreamAsync(Stream source, string fileName, CancellationToken cancellationToken = default)
        => ImportStreamAsync(source, fileName, cancellationToken, destPath: null);

    public async Task<PdfDocumentInfo?> ImportStreamAsync(
        Stream source,
        string fileName,
        CancellationToken cancellationToken,
        string? destPath)
    {
        fileName = PdfFileHelpers.SanitizeFileName(fileName);

        long size = source.CanSeek ? source.Length : 0;
        if (size > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"O ficheiro excede o limite de {FormatFileSize(MaxFileSizeBytes)}.");
        }

        var docsDir = Path.Combine(FileSystem.AppDataDirectory, "documents");
        Directory.CreateDirectory(docsDir);

        destPath ??= Path.Combine(docsDir, $"{Guid.NewGuid():N}_{fileName}");
        await using (var dest = File.Create(destPath))
        {
            await source.CopyToAsync(dest, cancellationToken);
        }

        var finalSize = new FileInfo(destPath).Length;
        if (finalSize > MaxFileSizeBytes)
        {
            File.Delete(destPath);
            throw new InvalidOperationException(
                $"O ficheiro excede o limite de {FormatFileSize(MaxFileSizeBytes)}.");
        }

        return await UpsertImportedFileAsync(fileName, destPath, finalSize, cancellationToken);
    }

    private async Task<PdfDocumentInfo> UpsertImportedFileAsync(
        string fileName,
        string destPath,
        long finalSize,
        CancellationToken cancellationToken)
    {
        var info = new PdfDocumentInfo
        {
            FileName = fileName,
            LocalPath = destPath,
            OpenedAtUtc = DateTime.UtcNow,
            FileSizeBytes = finalSize,
            LastPage = 1,
            LastZoom = 1.0,
        };

        return await _recentStore.UpsertAsync(info, cancellationToken);
    }

    public async Task<byte[]?> ReadBytesAsync(PdfDocumentInfo document, CancellationToken cancellationToken = default)
    {
        if (!document.FileExists)
        {
            return null;
        }

        var info = new FileInfo(document.LocalPath);
        if (info.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"O ficheiro excede o limite de {FormatFileSize(MaxFileSizeBytes)}.");
        }

        return await File.ReadAllBytesAsync(document.LocalPath, cancellationToken);
    }

    public Stream? OpenRead(PdfDocumentInfo document)
    {
        if (!document.FileExists)
        {
            return null;
        }

        var info = new FileInfo(document.LocalPath);
        if (info.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"O ficheiro excede o limite de {FormatFileSize(MaxFileSizeBytes)}.");
        }

        return File.OpenRead(document.LocalPath);
    }

    public async Task ShareAsync(PdfDocumentInfo document, CancellationToken cancellationToken = default)
    {
        if (!document.FileExists)
        {
            throw new InvalidOperationException("Ficheiro não encontrado.");
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = document.FileName,
            File = new ShareFile(document.LocalPath),
        });
    }

    public string FormatFileSize(long bytes) => PdfFileHelpers.FormatFileSize(bytes);

    private static readonly FilePickerFileType PdfFileTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.WinUI, [".pdf"] },
        { DevicePlatform.Android, ["application/pdf"] },
        { DevicePlatform.iOS, ["com.adobe.pdf"] },
        { DevicePlatform.MacCatalyst, ["com.adobe.pdf", ".pdf"] },
        { DevicePlatform.Tizen, ["*/*"] },
    });
}
