namespace OlondongeApp.Services;

public interface IGradesSyncService
{
    /// <summary>Disparado após cada tentativa de sincronização (UI offline-first).</summary>
    event Action<GradesSyncOutcome>? SyncFinished;

    /// <param name="force"><c>true</c> apenas quando o utilizador pede sincronização (⟳ / botões «Sincronizar»): chama a API e grava em SQLite e cache. <c>false</c> não faz pedidos HTTP.</param>
    Task<GradesSyncOutcome> RunScheduledSyncAsync(bool force = false, CancellationToken cancellationToken = default);

    /// <summary>UTC da última passagem completa do pipeline (qualquer outcome que carimbe o ticks). Null se nunca correu.</summary>
    DateTime? GetLastSyncUtc();
}

public sealed record GradesSyncOutcome(GradesSyncStatus Status, string? Message = null, bool DataChanged = false)
{
    public static GradesSyncOutcome TooSoon() => new(GradesSyncStatus.SkippedTooSoon);

    public static GradesSyncOutcome NoNetwork() => new(GradesSyncStatus.SkippedNoNetwork);

    public static GradesSyncOutcome NoChange() => new(GradesSyncStatus.SkippedNoChangeOnServer);

    public static GradesSyncOutcome Failed(string message) => new(GradesSyncStatus.Failed, message);

    public static GradesSyncOutcome Success(bool dataChanged) => new(GradesSyncStatus.Completed, null, dataChanged);
}

public enum GradesSyncStatus
{
    Completed,
    SkippedTooSoon,
    SkippedNoNetwork,
    SkippedNoChangeOnServer,
    Failed,
}
