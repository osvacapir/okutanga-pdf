using Microsoft.Extensions.DependencyInjection;
using OkutangaPDF.Models;
using OkutangaPDF.Services;

namespace OkutangaPDF;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        TryCaptureLaunchPdf(activationState);
        return new Window(new MainPage()) { Title = AppPublisherInfo.AppName };
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
            var recent = AppServices.Provider.GetRequiredService<IRecentDocumentsStore>();
            await recent.EnsureReadyAsync().ConfigureAwait(false);

            if (IncomingPdfBridge.HasBridgePending)
            {
                var incoming = AppServices.Provider.GetRequiredService<IIncomingPdfService>();
                await IncomingPdfBridge.FlushToServiceAsync(incoming).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("okutangaPDF startup: " + ex);
        }
    }

    private static void TryCaptureLaunchPdf(IActivationState? activationState)
    {
#if WINDOWS
        var args = Environment.GetCommandLineArgs();
        foreach (var arg in args)
        {
            if (arg.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && File.Exists(arg))
            {
                IncomingPdfBridge.SetPendingPath(arg);
                break;
            }
        }
#endif
    }
}
