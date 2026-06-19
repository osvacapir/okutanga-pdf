namespace OlondongeApp;

/// <summary>
/// Fornece o <see cref="IServiceProvider"/> raiz após o build MAUI (ex.: inicialização em <see cref="Application.OnStart"/>).
/// </summary>
public static class AppServices
{
    public static IServiceProvider? Provider { get; set; }
}
