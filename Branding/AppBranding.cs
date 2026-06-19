namespace OlondongeApp.Branding;

/// <summary>
/// URLs relativos ao wwwroot para o logo do sistema (ficheiros copiados de <c>Resources/Images</c> no build).
/// Ordem: raster primeiro (melhor no WebView); SVG por último.
/// </summary>
public static class AppBranding
{
    public static readonly string[] LogoWebRelativePaths =
    [
        "images/logo_system.png",
        "images/logo_system.webp",
        "images/logo_system.jpg",
        "images/logo_system.jpeg",
        "images/logo_system.svg",
    ];
}
