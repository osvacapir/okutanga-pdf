namespace OkutangaPDF.Services;

/// <summary>
/// Expõe a pasta de documentos ao WebView via URL local (evita copiar o PDF para o JavaScript).
/// Windows: WebView2 <c>SetVirtualHostNameToFolderMapping</c>.
/// </summary>
public static class PdfLocalViewerHost
{
    public const string VirtualHost = "okutanga.app";

#if WINDOWS
    private static volatile bool _configured;
    private static readonly TaskCompletionSource<bool> ReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
#endif

    public static bool IsConfigured =>
#if WINDOWS
        _configured;
#else
        false;
#endif

    public static string DocumentsDirectory =>
        Path.Combine(FileSystem.AppDataDirectory, "documents");

    public static Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        if (IsConfigured)
        {
            return Task.CompletedTask;
        }

#if WINDOWS
        return ReadyTcs.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
#else
        return Task.CompletedTask;
#endif
    }

    public static void TryConfigure(object? platformWebView)
    {
#if WINDOWS
        TryConfigureWindows(platformWebView);
#endif
    }

#if WINDOWS
    private static void TryConfigureWindows(object? webView)
    {
        if (webView is not Microsoft.UI.Xaml.Controls.WebView2 wv2)
        {
            return;
        }

        void MapHost()
        {
            if (_configured || wv2.CoreWebView2 is null)
            {
                return;
            }

            Directory.CreateDirectory(DocumentsDirectory);
            wv2.CoreWebView2.SetVirtualHostNameToFolderMapping(
                VirtualHost,
                DocumentsDirectory,
                Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
            _configured = true;
            ReadyTcs.TrySetResult(true);
        }

        if (wv2.CoreWebView2 is not null)
        {
            MapHost();
        }

        wv2.CoreWebView2Initialized += (_, _) => MapHost();
    }
#endif

    public static string? TryGetDocumentUrl(string localPath)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(localPath);
        var docsRoot = Path.GetFullPath(DocumentsDirectory);
        if (!fullPath.StartsWith(docsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var fileName = Path.GetFileName(fullPath);
        return $"https://{VirtualHost}/{Uri.EscapeDataString(fileName)}";
    }
}
