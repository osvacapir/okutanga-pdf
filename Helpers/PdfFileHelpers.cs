namespace OkutangaPDF.Helpers;

public static class PdfFileHelpers
{
    public const long DefaultMaxFileSizeBytes = 30L * 1024 * 1024;

    public static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{bytes / 1024.0:0.#} KB");
        }

        return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{bytes / (1024.0 * 1024.0):0.#} MB");
    }

    public static string SanitizeFileName(string? name)
    {
        var raw = string.IsNullOrWhiteSpace(name) ? "documento.pdf" : name.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            raw = raw.Replace(c, '_');
        }

        return raw.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? raw : raw + ".pdf";
    }
}
