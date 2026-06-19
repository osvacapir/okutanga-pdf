namespace OlondongeApp.Services;

public interface IAuthService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    bool IsReady { get; }

    bool IsAuthenticated { get; }

    string? DisplayName { get; }

    /// <summary>Número de processo do aluno (vindo do login), quando disponível.</summary>
    string? NumProcesso { get; }

    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    Task<AuthLoginResult> LoginAsync(string identifier, string password, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>Elimina notas, matrículas e meta de sincronização em SQLite; limpa marcações de sync.
    /// Não encerra sessão.</summary>
    Task ClearOfflineCacheAsync(CancellationToken cancellationToken = default);

    event Action? AuthenticationStateChanged;
}

/// <summary>Motivo da falha do login, para a UI escolher uma mensagem amigável sem expor detalhes técnicos.</summary>
public enum LoginFailureReason
{
    None = 0,

    /// <summary>O utilizador/identificador ou a palavra-passe não conferem (401).</summary>
    InvalidCredentials,

    /// <summary>Falha de transporte HTTP. A UI deve confirmar com Connectivity se há Internet antes de decidir a mensagem.</summary>
    Network,

    /// <summary>Servidor respondeu fora do esperado (5xx, JSON inválido, timeout, etc.).</summary>
    ServiceUnavailable,
}

/// <param name="RequestCorrelationId">Presente em falhas para cruzar com Nginx (<c>X-Request-Id</c>) / suporte; vai apenas para logs, nunca para a UI.</param>
public sealed record AuthLoginResult(
    bool Success,
    string? ErrorMessage,
    string? RequestCorrelationId = null,
    LoginFailureReason Reason = LoginFailureReason.None);
