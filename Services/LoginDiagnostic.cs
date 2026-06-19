namespace OlondongeApp.Services;

/// <summary>
/// Diagnóstico de login: visível em <c>adb logcat -s OlondongeAuth:I</c> (Android Release) e via ILogger.
/// Não regista identificadores completos nem tokens.
/// </summary>
internal static class LoginDiagnostic
{
    private const int MaxSnippet = 280;
    private const int MaxDeviceLine = 3500;

    public static string SafeHostFromBaseUri(string baseUri)
    {
        if (!Uri.TryCreate(baseUri, UriKind.Absolute, out var u))
        {
            return "(invalid-uri)";
        }

        return u.Host;
    }

    public static string BodySnippet(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return "(empty-body)";
        }

        var t = body.Trim();
        if (t.Contains("access_token", StringComparison.OrdinalIgnoreCase)
            || t.Contains("accessToken", StringComparison.OrdinalIgnoreCase))
        {
            return "(body-redacted-may-contain-token)";
        }

        t = OneLine(t);
        return t.Length <= MaxSnippet ? t : t[..MaxSnippet] + "…";
    }

    public static string OneLine(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace('\r', ' ').Replace('\n', ' ').Trim();
    }

    public static void WriteDevice(string tag, string message)
    {
        var line = message.Length > MaxDeviceLine ? message[..MaxDeviceLine] + "…" : message;
#if ANDROID
        global::Android.Util.Log.Info(tag, line);
#else
        System.Diagnostics.Debug.WriteLine($"[{tag}] {line}");
#endif
    }
}
