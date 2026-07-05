using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OkutangaPDF.Services;

namespace OkutangaPDF;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        SQLitePCL.Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddSingleton<IAppShellService, AppShellService>();
        builder.Services.AddSingleton<IRecentDocumentsStore, SqliteRecentDocumentsStore>();
        builder.Services.AddSingleton<IPdfBookmarkStore, SqlitePdfBookmarkStore>();
        builder.Services.AddSingleton<IPdfFileService, PdfFileService>();
        builder.Services.AddSingleton<IPdfReaderSession, PdfReaderSession>();
        builder.Services.AddSingleton<IPdfReaderSettingsService, PdfReaderSettingsService>();
        builder.Services.AddSingleton<IKeepAwakeService, KeepAwakeService>();
        builder.Services.AddSingleton<IIncomingPdfService, IncomingPdfService>();
        builder.Services.AddSingleton<IAppNotificationService, AppNotificationService>();

#if DEBUG && !ANDROID
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif
#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        AppServices.Provider = app.Services;
        return app;
    }
}
