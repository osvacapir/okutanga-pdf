namespace OkutangaPDF.Models;

public sealed class PdfDocumentInfo
{
    public int Id { get; init; }
    public required string FileName { get; init; }
    public required string LocalPath { get; init; }
    public DateTime OpenedAtUtc { get; init; }
    public int PageCount { get; set; }
    public int LastPage { get; set; } = 1;
    public long FileSizeBytes { get; init; }
    public double LastZoom { get; set; } = 1.0;

    public bool FileExists => File.Exists(LocalPath);
}
