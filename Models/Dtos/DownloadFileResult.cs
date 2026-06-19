namespace OlondongeApp.Models.Dtos;

public sealed class DownloadFileResult
{
    public required byte[] Content { get; init; }

    public required string FileName { get; init; }

    public string ContentType { get; init; } = "application/octet-stream";
}

