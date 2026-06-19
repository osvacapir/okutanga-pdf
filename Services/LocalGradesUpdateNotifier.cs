using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.ApplicationModel;

namespace OlondongeApp.Services;

/// <summary>
/// Notificação local quando a sincronização deteta notas novas (Android nativo; outras plataformas: no-op).
/// </summary>
public sealed class LocalGradesUpdateNotifier : IGradesUpdateNotifier
{
    private const int NotificationId = 91001;

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public Task EnsureNotificationPermissionAsync(CancellationToken cancellationToken = default)
    {
#if ANDROID
        try
        {
            if (global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.Tiramisu)
            {
                return Task.CompletedTask;
            }

            var activity = Platform.CurrentActivity;
            if (activity is null)
            {
                return Task.CompletedTask;
            }

            if (global::AndroidX.Core.Content.ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.PostNotifications)
                == global::Android.Content.PM.Permission.Granted)
            {
                return Task.CompletedTask;
            }

            global::AndroidX.Core.App.ActivityCompat.RequestPermissions(
                activity,
                new[] { global::Android.Manifest.Permission.PostNotifications },
                1002);
        }
        catch
        {
            // ignore
        }
#endif
        return Task.CompletedTask;
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public Task NotifyGradesUpdatedAsync(CancellationToken cancellationToken = default)
    {
#if ANDROID
        try
        {
            var context = global::Android.App.Application.Context;
            if (context is null)
            {
                return Task.CompletedTask;
            }

            const string channelId = "olondonge_grades";
            var nm = context.GetSystemService(global::Android.Content.Context.NotificationService) as global::Android.App.NotificationManager;
            if (nm is null)
            {
                return Task.CompletedTask;
            }

            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                var channel = new global::Android.App.NotificationChannel(
                    channelId,
                    "Notas Olondonge",
                    global::Android.App.NotificationImportance.Default);
                nm.CreateNotificationChannel(channel);
            }

            var compatBuilder = new global::AndroidX.Core.App.NotificationCompat.Builder(context!, channelId);
            if (compatBuilder is null)
            {
                return Task.CompletedTask;
            }

            compatBuilder.SetContentTitle("Notas atualizadas");
            compatBuilder.SetContentText("Existem novas notas sincronizadas. Abra a app para consultar.");
            compatBuilder.SetAutoCancel(true);
            compatBuilder.SetPriority(global::AndroidX.Core.App.NotificationCompat.PriorityDefault);

            var icon = context.ApplicationInfo?.Icon ?? 0;
            if (icon != 0)
            {
                compatBuilder.SetSmallIcon(icon);
            }

            nm!.Notify(NotificationId, compatBuilder.Build());
        }
        catch
        {
            // não bloquear sync por falha de notificação
        }
#endif
        return Task.CompletedTask;
    }
}
