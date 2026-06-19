using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using OlondongeApp.Configuration;
using OlondongeApp.Services;

namespace OlondongeApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        SQLitePCL.Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();

        var assembly = typeof(MauiProgram).Assembly;

        void AddEmbeddedJson(string fileName)
        {
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.Ordinal));
            if (resourceName is null)
            {
                return;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                builder.Configuration.AddJsonStream(stream);
            }
        }

        AddEmbeddedJson("appsettings.json");
#if !DEBUG
        // Release: sobrepõe com appsettings.Production.json (URL/tenant). Em Debug não carregar evita overrides acidentais (ex.: WindowsDockerApiPort = 0).
        AddEmbeddedJson("appsettings.Production.json");
#endif

        builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));
        builder.Services.PostConfigure<ApiOptions>(o =>
        {
            o.BaseUrl = (o.BaseUrl ?? string.Empty).Trim();
            o.TenantDomain = (o.TenantDomain ?? string.Empty).Trim();
            o.TenantSlug = (o.TenantSlug ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(o.BaseUrl))
            {
                o.BaseUrl = "http://med-api.localhost/api/v1/";
            }

#if ANDROID
            // Emulador (Debug): reescrever anfitriões locais para 10.0.2.2. Não alterar URLs de produção (https, domínio real).
            // Quando o stack dev usa Traefik (porta 80, rota por Host), a regra do router deve aceitar o host usado.
            // Para nginx exposto directamente sem Traefik, ajustar BaseUrl no appsettings para incluir a porta (ex.: :8081).
            //
            // Dispositivo físico: 10.0.2.2 só existe no emulador. Em aparelho real usa-se o IP LAN do PC,
            // configurável em ApiOptions.AndroidPhysicalDeviceHost.
            var bu = o.BaseUrl.Trim();
            var isPhysicalDevice = false;
            try
            {
                isPhysicalDevice = DeviceInfo.Current.DeviceType == DeviceType.Physical;
            }
            catch
            {
                // DeviceInfo pode falhar em arranque muito cedo / testes; assumir emulador.
            }

            var androidHost = isPhysicalDevice && !string.IsNullOrWhiteSpace(o.AndroidPhysicalDeviceHost)
                ? o.AndroidPhysicalDeviceHost.Trim()
                : "10.0.2.2";

            var isLocalDevHost = bu.Contains("med-api.localhost", StringComparison.OrdinalIgnoreCase)
                || bu.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || bu.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                || bu.Contains("10.0.2.2", StringComparison.OrdinalIgnoreCase);
            if (isLocalDevHost && bu.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                if (bu.Contains("med-api.localhost", StringComparison.OrdinalIgnoreCase))
                {
                    o.BaseUrl = bu.Replace("med-api.localhost", androidHost, StringComparison.OrdinalIgnoreCase);
                }
                else if (bu.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                {
                    o.BaseUrl = bu.Replace("127.0.0.1", androidHost, StringComparison.OrdinalIgnoreCase);
                }
                else if (bu.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    o.BaseUrl = bu.Replace("localhost", androidHost, StringComparison.OrdinalIgnoreCase);
                }
                else if (bu.Contains("10.0.2.2", StringComparison.OrdinalIgnoreCase) && isPhysicalDevice && !string.Equals(androidHost, "10.0.2.2", StringComparison.OrdinalIgnoreCase))
                {
                    o.BaseUrl = bu.Replace("10.0.2.2", androidHost, StringComparison.OrdinalIgnoreCase);
                }
            }
#elif WINDOWS || MACCATALYST
            // WinUI / Mac: "med-api.localhost" muitas vezes não resolve (DNS 11001). Usar loopback + porta do Docker.
            var desktopBase = o.BaseUrl.Trim();
            if (desktopBase.Contains("med-api.localhost", StringComparison.OrdinalIgnoreCase))
            {
                desktopBase = desktopBase.Replace("med-api.localhost", "127.0.0.1", StringComparison.OrdinalIgnoreCase);
                if (Uri.TryCreate(desktopBase, UriKind.Absolute, out var desktopUri)
                    && desktopUri.Scheme == Uri.UriSchemeHttp
                    && desktopUri.Port == 80
                    && o.WindowsDockerApiPort > 0)
                {
                    var ub = new UriBuilder(desktopUri) { Port = o.WindowsDockerApiPort };
                    desktopBase = ub.Uri.ToString().TrimEnd('/');
                }

                o.BaseUrl = desktopBase.EndsWith('/') ? desktopBase : desktopBase + "/";
            }
            else if (Uri.TryCreate(desktopBase, UriKind.Absolute, out var loopUri)
                && loopUri.Scheme == Uri.UriSchemeHttp
                && loopUri.Port == 80
                && o.WindowsDockerApiPort > 0
                && (loopUri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || loopUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)))
            {
                // http://127.0.0.1/api/v1/ sem serviço na porta 80 → usar porta do nginx no Docker (WindowsDockerApiPort).
                var ub = new UriBuilder(loopUri) { Port = o.WindowsDockerApiPort };
                desktopBase = ub.Uri.ToString().TrimEnd('/');
                o.BaseUrl = desktopBase.EndsWith('/') ? desktopBase : desktopBase + "/";
            }
#endif
        });

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<IDeviceInfoService, DeviceInfoService>();

        builder.Services.AddSingleton<IAppNotificationService, AppNotificationService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IGradesLocalStore, SqliteGradesLocalStore>();
        builder.Services.AddSingleton<IGradesUpdateNotifier, LocalGradesUpdateNotifier>();
        builder.Services.AddTransient<AuthDelegatingHandler>();
        builder.Services.AddHttpClient<IStudentGradesApi, StudentGradesApi>((sp, client) =>
            {
                var opt = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
                var baseUrl = opt.BaseUrl.TrimEnd('/') + "/";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(Math.Clamp(opt.RequestTimeoutSeconds, 15, 300));
                // Sem isto, alguns proxies/Laravel podem responder 302/HTML em vez de JSON às rotas API.
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            })
            .AddHttpMessageHandler<AuthDelegatingHandler>();
        builder.Services.AddSingleton<IGradesSyncService, GradesSyncService>();
        builder.Services.AddSingleton<IStudentAcademicOverviewService, StudentAcademicOverviewService>();

#if DEBUG && !ANDROID
        // Android físico 32-bit: estas DLLs (HotReload, MobileTap) aumentam muito o JIT no arranque e agravam ecrã preto.
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif
#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Login / auth: útil em Release Android via adb logcat (OlondongeAuth) + canal ILogger.
        builder.Logging.AddFilter("OlondongeApp.Services.AuthService", LogLevel.Information);

        var app = builder.Build();
        AppServices.Provider = app.Services;
        return app;
    }
}
