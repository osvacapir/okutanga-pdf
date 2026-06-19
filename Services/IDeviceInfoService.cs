using Microsoft.Maui.Devices;

namespace OlondongeApp.Services;

public interface IDeviceInfoService
{
    bool IsTablet { get; }
    bool IsPhone { get; }
    DeviceIdiom DeviceIdiom { get; }
}

