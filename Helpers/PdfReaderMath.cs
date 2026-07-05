namespace OkutangaPDF.Helpers;

public static class PdfReaderMath
{
    public const double MinScale = 0.5;
    public const double MaxScale = 3.0;
    public const double MinStoredZoom = 0.5;
    public const double MaxStoredZoom = 3.0;

    public static double ClampScale(double scale) => Math.Clamp(scale, MinScale, MaxScale);

    public static double ClampStoredZoom(double zoom) => Math.Clamp(zoom, MinStoredZoom, MaxStoredZoom);

    public static int ClampPage(int page, int pageCount)
    {
        if (pageCount < 1)
        {
            return 1;
        }

        return Math.Clamp(page, 1, pageCount);
    }

    public static int ClampRecentLimit(int limit) => Math.Clamp(limit, 1, 200);
}
