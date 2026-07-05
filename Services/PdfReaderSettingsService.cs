using OkutangaPDF.Models;

namespace OkutangaPDF.Services;

public interface IPdfReaderSettingsService
{
    PdfReaderSettings Get();

    void Save(PdfReaderSettings settings);

    event Action? SettingsChanged;
}

public sealed class PdfReaderSettingsService : IPdfReaderSettingsService
{
    private const string ScrollModeKey = "ok_pdf_scroll_mode";
    private const string KeepScreenOnKey = "ok_pdf_keep_screen_on";
    private const string DefaultZoomKey = "ok_pdf_default_zoom";
    private const string RememberZoomKey = "ok_pdf_remember_zoom";

    public event Action? SettingsChanged;

    public PdfReaderSettings Get() => new()
    {
        ScrollMode = Enum.TryParse<PdfScrollMode>(Preferences.Get(ScrollModeKey, PdfScrollMode.Continuous.ToString()), out var mode)
            ? mode
            : PdfScrollMode.Continuous,
        KeepScreenOn = Preferences.Get(KeepScreenOnKey, false),
        DefaultZoom = Preferences.Get(DefaultZoomKey, 1.0),
        RememberZoom = Preferences.Get(RememberZoomKey, true),
    };

    public void Save(PdfReaderSettings settings)
    {
        Preferences.Set(ScrollModeKey, settings.ScrollMode.ToString());
        Preferences.Set(KeepScreenOnKey, settings.KeepScreenOn);
        Preferences.Set(DefaultZoomKey, Math.Clamp(settings.DefaultZoom, 0.5, 3.0));
        Preferences.Set(RememberZoomKey, settings.RememberZoom);
        SettingsChanged?.Invoke();
    }
}
