namespace OlondongeApp.Configuration;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    /// <summary>
    /// Base URL da API (terminar com /), ex.: https://api.escola.ao/api/v1/
    /// </summary>
    public string BaseUrl { get; set; } = "http://med-api.localhost/api/v1/";

    public string TenantDomain { get; set; } = string.Empty;

    public string TenantSlug { get; set; } = string.Empty;

    public int RequestTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Em Windows/Mac Catalyst, ao reescrever <c>med-api.localhost</c> para <c>127.0.0.1</c>, porta HTTP do nginx no host
    /// (ver <c>APP_PORT</c> no docker-compose da api-med). Use 0 quando o API está na porta 80 no host (ex.: Traefik em :80).
    /// </summary>
    public int WindowsDockerApiPort { get; set; } = 8081;

    /// <summary>
    /// IP/host LAN do PC que serve a API, usado por <em>dispositivos Android físicos</em> ao reescrever <c>med-api.localhost</c>.
    /// Deixar vazio para usar a regra do emulador (<c>10.0.2.2</c>). Exemplo: <c>10.39.121.70</c> ou <c>192.168.1.50</c>.
    /// Em Traefik, garantir que a regra do router aceita este host (ex.: <c>Host(`10.39.121.70`)</c>).
    /// </summary>
    public string AndroidPhysicalDeviceHost { get; set; } = string.Empty;
}
