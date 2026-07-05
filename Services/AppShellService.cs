using Microsoft.Maui.Devices;

namespace OkutangaPDF.Services;

public sealed class AppShellService : IAppShellService
{
    public bool IsDesktopShell
    {
        get
        {
            var idiom = DeviceInfo.Idiom;
            return idiom == DeviceIdiom.Desktop || idiom == DeviceIdiom.Tablet;
        }
    }

    public bool UseCompactReaderUi => DeviceInfo.Idiom == DeviceIdiom.Phone;

    public DeviceIdiom DeviceIdiom => DeviceInfo.Idiom;
}
