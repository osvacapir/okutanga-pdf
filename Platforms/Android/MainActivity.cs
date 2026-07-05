using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using OkutangaPDF.Services;
using Microsoft.Maui.ApplicationModel;
using AndroidUri = Android.Net.Uri;

namespace OkutangaPDF;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataMimeType = "application/pdf")]
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "application/pdf")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandleIntent(intent);
    }

    private static void HandleIntent(Intent? intent)
    {
        if (intent?.Action is not (Intent.ActionView or Intent.ActionSend))
        {
            return;
        }

        var uri = intent.Data;
        if (uri is null)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                uri = intent.GetParcelableExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(AndroidUri))) as AndroidUri;
            }
            else
            {
#pragma warning disable CA1422 // GetParcelableExtra(string) obsoleto em API 33+; ramo só em API 24–32.
                uri = intent.GetParcelableExtra(Intent.ExtraStream) as AndroidUri;
#pragma warning restore CA1422
            }
        }

        if (uri is null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"okutanga_in_{Guid.NewGuid():N}.pdf");
            try
            {
                var fileName = ResolveFileName(uri);
                var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("Activity indisponível");
                await using (var input = activity.ContentResolver!.OpenInputStream(uri)!)
                await using (var tempFile = File.Create(tempPath))
                {
                    await input.CopyToAsync(tempFile);
                }

                if (AppServices.Provider is not null)
                {
                    var incoming = AppServices.Provider.GetRequiredService<IIncomingPdfService>();
                    await using var importStream = File.OpenRead(tempPath);
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                        await incoming.EnqueueFromStreamAsync(importStream, fileName));
                }
                else
                {
                    var pendingPath = Path.Combine(Path.GetTempPath(), $"okutanga_pending_{Guid.NewGuid():N}.pdf");
                    File.Move(tempPath, pendingPath);
                    tempPath = string.Empty;
                    IncomingPdfBridge.SetPendingPath(pendingPath);
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn("okutangaPDF", "Importação via intent falhou: " + ex.Message);
                var path = uri.Path;
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    IncomingPdfBridge.SetPendingPath(path);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempPath))
                {
                    try
                    {
                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }
                    }
                    catch
                    {
                        // temp opcional
                    }
                }
            }
        });
    }

    private static string ResolveFileName(AndroidUri uri)
    {
        var last = uri.LastPathSegment;
        if (!string.IsNullOrWhiteSpace(last) && last.Contains('.', StringComparison.Ordinal))
        {
            return last;
        }

        return "documento.pdf";
    }
}
