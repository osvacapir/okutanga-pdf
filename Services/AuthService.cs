using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OlondongeApp.Configuration;
using OlondongeApp.Models.Dtos;

namespace OlondongeApp.Services;

public sealed class AuthService : IAuthService
{
    private const string TokenKey = "olondonge_token";
    private const string UserNameKey = "olondonge_user_name";
    private const string NumProcessoKey = "olondonge_num_processo";
    private const string DeviceLogTag = "OlondongeAuth";

    private readonly IOptions<ApiOptions> _options;
    private readonly IGradesLocalStore _gradesLocalStore;
    private readonly ILogger<AuthService> _logger;

    private string? _cachedToken;
    private string? _displayName;
    private string? _numProcesso;
    private bool _initialized;

    public AuthService(IOptions<ApiOptions> options, IGradesLocalStore gradesLocalStore, ILogger<AuthService> logger)
    {
        _options = options;
        _gradesLocalStore = gradesLocalStore;
        _logger = logger;
    }

    public bool IsReady => _initialized;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_cachedToken);

    public string? DisplayName => _displayName;

    public string? NumProcesso => _numProcesso;

    public event Action? AuthenticationStateChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _cachedToken = await SecureStorage.Default.GetAsync(TokenKey).ConfigureAwait(false);
            _displayName = await SecureStorage.Default.GetAsync(UserNameKey).ConfigureAwait(false);
            _numProcesso = await SecureStorage.Default.GetAsync(NumProcessoKey).ConfigureAwait(false);
        }
        catch
        {
            _cachedToken = null;
            _displayName = null;
            _numProcesso = null;
        }

        _initialized = true;
    }

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_cachedToken);
    }

    public async Task<AuthLoginResult> LoginAsync(string identifier, string password, CancellationToken cancellationToken = default)
    {
        if (!_initialized)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        var correlationId = Guid.NewGuid().ToString("N")[..12];
        var baseUri = _options.Value.BaseUrl.TrimEnd('/') + '/';
        var apiHost = LoginDiagnostic.SafeHostFromBaseUri(baseUri);

        using var client = new HttpClient { BaseAddress = new Uri(baseUri), Timeout = TimeSpan.FromSeconds(_options.Value.RequestTimeoutSeconds) };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "OlondongeApp/1.0");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Request-Id", correlationId);
        ApplyTenantHeaders(client);

        var idLen = identifier.Trim().Length;
        _logger.LogInformation("Login início {CorrelationId} Host={Host} IdentifierLength={IdentifierLength}", correlationId, apiHost, idLen);
        LoginDiagnostic.WriteDevice(DeviceLogTag, $"login_start id={correlationId} host={apiHost} idLen={idLen}");

        try
        {
            using var response = await client.PostAsJsonAsync(
                    "auth/student/login",
                    new { identifier = identifier.Trim(), password },
                    AppJson.Options,
                    cancellationToken)
                .ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var snippet = LoginDiagnostic.BodySnippet(body);
                _logger.LogWarning(
                    "Login HTTP não-sucesso {CorrelationId} Status={StatusCode} Snippet={Snippet}",
                    correlationId,
                    (int)response.StatusCode,
                    snippet);
                LoginDiagnostic.WriteDevice(DeviceLogTag, $"login_http id={correlationId} status={(int)response.StatusCode} snippet={snippet}");
                var err = TryParseMessage(body) ?? "Credenciais inválidas ou erro no servidor.";
                var reason = response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                                  or System.Net.HttpStatusCode.Forbidden
                                                  or System.Net.HttpStatusCode.UnprocessableEntity
                    ? LoginFailureReason.InvalidCredentials
                    : LoginFailureReason.ServiceUnavailable;
                return new AuthLoginResult(false, err, correlationId, reason);
            }

            ApiResponse<LoginDataDto>? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<ApiResponse<LoginDataDto>>(body, AppJson.Options);
            }
            catch (JsonException ex)
            {
                var snippet = LoginDiagnostic.BodySnippet(body);
                _logger.LogWarning(ex, "Login JSON inválido {CorrelationId} Snippet={Snippet}", correlationId, snippet);
                LoginDiagnostic.WriteDevice(DeviceLogTag, $"login_json id={correlationId} snippet={snippet}");
                return new AuthLoginResult(false, "Resposta inválida do servidor.", correlationId, LoginFailureReason.ServiceUnavailable);
            }

            if (parsed?.Success != true || parsed.Data?.AccessToken is not { Length: > 0 } token)
            {
                var msg = parsed?.Message ?? "Login sem token.";
                _logger.LogWarning("Login rejeitado pela API {CorrelationId} Message={Message}", correlationId, msg);
                LoginDiagnostic.WriteDevice(DeviceLogTag, $"login_api_reject id={correlationId} msg={LoginDiagnostic.OneLine(msg)}");
                return new AuthLoginResult(false, msg, correlationId, LoginFailureReason.ServiceUnavailable);
            }

            _cachedToken = token;
            _displayName = parsed.Data.User?.Name;
            _numProcesso = parsed.Data.User?.NumProcesso?.Trim();

            try
            {
                await SecureStorage.Default.SetAsync(TokenKey, token).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(_displayName))
                {
                    await SecureStorage.Default.SetAsync(UserNameKey, _displayName).ConfigureAwait(false);
                }

                if (!string.IsNullOrEmpty(_numProcesso))
                {
                    await SecureStorage.Default.SetAsync(NumProcessoKey, _numProcesso).ConfigureAwait(false);
                }
                else
                {
                    SecureStorage.Default.Remove(NumProcessoKey);
                }
            }
            catch
            {
                // SecureStorage pode falhar em alguns simuladores; mantém sessão em memória
            }

            AuthenticationStateChanged?.Invoke();
            _logger.LogInformation("Login concluído com sucesso {CorrelationId} Host={Host}", correlationId, apiHost);
            LoginDiagnostic.WriteDevice(DeviceLogTag, $"login_ok id={correlationId} host={apiHost}");
            return new AuthLoginResult(true, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Login timeout {CorrelationId}", correlationId);
            LoginDiagnostic.WriteDevice(DeviceLogTag, $"login_timeout id={correlationId}");
            return new AuthLoginResult(false, "Timeout no servidor.", correlationId, LoginFailureReason.ServiceUnavailable);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Login falha de rede {CorrelationId} Host={Host}", correlationId, apiHost);
            LoginDiagnostic.WriteDevice(DeviceLogTag, $"login_network id={correlationId} host={apiHost} err={LoginDiagnostic.OneLine(ex.Message)}");
            return new AuthLoginResult(false, "Falha de rede.", correlationId, LoginFailureReason.Network);
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_cachedToken))
        {
            try
            {
                var baseUri = _options.Value.BaseUrl.TrimEnd('/') + '/';
                using var client = new HttpClient { BaseAddress = new Uri(baseUri), Timeout = TimeSpan.FromSeconds(_options.Value.RequestTimeoutSeconds) };
                using var req = new HttpRequestMessage(HttpMethod.Post, "auth/student/logout");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _cachedToken);
                ApplyTenantHeadersToRequest(req);
                await client.SendAsync(req, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // ignora falha de rede no logout
            }
        }

        _cachedToken = null;
        _displayName = null;
        _numProcesso = null;

        try
        {
            SecureStorage.Default.Remove(TokenKey);
            SecureStorage.Default.Remove(UserNameKey);
            SecureStorage.Default.Remove(NumProcessoKey);
        }
        catch
        {
            // ignore
        }

        try
        {
            ClearOlondongeSyncPreferences();
            await _gradesLocalStore.ClearAllAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }

        AuthenticationStateChanged?.Invoke();
    }

    public async Task ClearOfflineCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ClearOlondongeSyncPreferences();
            await _gradesLocalStore.ClearAllAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // dados locais já podem estar vazios ou inacessíveis
        }

        AuthenticationStateChanged?.Invoke();
    }

    private static void ClearOlondongeSyncPreferences()
    {
        Preferences.Default.Remove(OlondongePreferenceKeys.LastSyncPipelineTicks);
        Preferences.Default.Remove(OlondongePreferenceKeys.SavedGradesVersion);
        Preferences.Default.Remove(OlondongePreferenceKeys.LastEnrolmentsRefreshTicks);
        Preferences.Default.Remove(OlondongePreferenceKeys.EnrolmentsApiContractVersion);
    }

    private void ApplyTenantHeaders(HttpClient client)
    {
        var o = _options.Value;
        if (!string.IsNullOrWhiteSpace(o.TenantDomain))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-Domain", o.TenantDomain);
        }

        if (!string.IsNullOrWhiteSpace(o.TenantSlug))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-Slug", o.TenantSlug);
        }
    }

    private void ApplyTenantHeadersToRequest(HttpRequestMessage request)
    {
        var o = _options.Value;
        if (!string.IsNullOrWhiteSpace(o.TenantDomain))
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Domain", o.TenantDomain);
        }

        if (!string.IsNullOrWhiteSpace(o.TenantSlug))
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Slug", o.TenantSlug);
        }
    }

    private static string? TryParseMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var m))
            {
                return m.GetString();
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }
}
