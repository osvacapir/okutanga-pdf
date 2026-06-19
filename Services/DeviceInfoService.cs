using Microsoft.Maui.Devices;

namespace OlondongeApp.Services;

public sealed class DeviceInfoService : IDeviceInfoService
{
    public bool IsTablet => DeviceInfo.Idiom == DeviceIdiom.Tablet;
    public bool IsPhone => DeviceInfo.Idiom == DeviceIdiom.Phone;
    public DeviceIdiom DeviceIdiom => DeviceInfo.Idiom;
}

