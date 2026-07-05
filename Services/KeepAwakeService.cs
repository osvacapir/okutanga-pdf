namespace OkutangaPDF.Services;

public interface IKeepAwakeService
{
    void SetActive(bool active);
}

public sealed class KeepAwakeService : IKeepAwakeService
{
    public void SetActive(bool active)
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        if (activity?.Window is null)
        {
            return;
        }

        if (active)
        {
            activity.Window.AddFlags(Android.Views.WindowManagerFlags.KeepScreenOn);
        }
        else
        {
            activity.Window.ClearFlags(Android.Views.WindowManagerFlags.KeepScreenOn);
        }
#elif IOS || MACCATALYST
        UIKit.UIApplication.SharedApplication.IdleTimerDisabled = active;
#elif WINDOWS
        // WinUI: sem API simples cross-thread; ignorar no MVP desktop.
#endif
    }
}
