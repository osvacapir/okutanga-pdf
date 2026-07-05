namespace OkutangaPDF.Models;

public enum PdfScrollMode
{
    Continuous,
    SinglePage,
}

public sealed class PdfReaderSettings
{
    public PdfScrollMode ScrollMode { get; set; } = PdfScrollMode.Continuous;

    public bool KeepScreenOn { get; set; }

    public double DefaultZoom { get; set; } = 1.0;

    public bool RememberZoom { get; set; } = true;
}
