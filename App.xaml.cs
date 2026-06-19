using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using OlondongeApp.Services;

namespace OlondongeApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "Olondonge" };
    }

    protected override async void OnStart()
    {
        base.OnStart();
        if (AppServices.Provider is null)
        {
            return;
        }

        try
        {
            var auth = AppServices.Provider.GetRequiredService<IAuthService>();
            await auth.InitializeAsync().ConfigureAwait(false);
            var store = AppServices.Provider.GetRequiredService<IGradesLocalStore>();
            await store.EnsureReadyAsync().ConfigureAwait(false);
            var notifier = AppServices.Provider.GetRequiredService<IGradesUpdateNotifier>();
            // Android: RequestPermissions tem de correr na thread de UI; após ConfigureAwait(false) podemos estar na pool.
            await MainThread.InvokeOnMainThreadAsync(() => notifier.EnsureNotificationPermissionAsync()).ConfigureAwait(false);
        }
        catch
        {
            // evita crash na arranque se serviços falharem
        }
    }
}
