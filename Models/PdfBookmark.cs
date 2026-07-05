namespace OkutangaPDF.Models;

public sealed class PdfBookmark
{
    public int Id { get; init; }
    public int DocumentId { get; init; }
    public int PageNumber { get; init; }
    public string? Label { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class PdfSearchMatch
{
    public int MatchIndex { get; init; }
    public int PageNumber { get; init; }
    public required string Snippet { get; init; }
}
