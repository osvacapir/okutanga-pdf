using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;
using OlondongeApp.Models.Dtos;

namespace OlondongeApp.Services;

public sealed class GradesSyncService : IGradesSyncService
{
    private const string CurrentYearSyncMarkerKey = "enrolments_current_year_synced";

    /// <summary>Subir quando o contrato de <c>student/enrolments</c> ou campos persistidos em SQLite mudarem, para não ficar preso a <c>NoChange</c> com a mesma GradesVersion.</summary>
    private const int RequiredEnrolmentsApiContractVersion = 3;

    private readonly IStudentGradesApi _api;
    private readonly IGradesLocalStore _store;
    private readonly IGradesUpdateNotifier _notifier;
    private readonly ILogger<GradesSyncService> _logger;
    private readonly SemaphoreSlim _syncRunLock = new(1, 1);

    public event Action<GradesSyncOutcome>? SyncFinished;

    public GradesSyncService(
        IStudentGradesApi api,
        IGradesLocalStore store,
        IGradesUpdateNotifier notifier,
        ILogger<GradesSyncService> logger)
    {
        _api = api;
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    public DateTime? GetLastSyncUtc()
    {
        var ticks = Preferences.Default.Get(OlondongePreferenceKeys.LastSyncPipelineTicks, 0L);
        if (ticks <= 0)
        {
            return null;
        }

        return new DateTime(ticks, DateTimeKind.Utc);
    }

    public async Task<GradesSyncOutcome> RunScheduledSyncAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        await _syncRunLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            GradesSyncOutcome outcome;
            try
            {
                outcome = await RunInternalAsync(force, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                outcome = GradesSyncOutcome.Failed("Tempo de espera da rede excedido ou sincronização interrompida. Tente novamente.");
            }
            catch (Exception ex)
            {
                outcome = GradesSyncOutcome.Failed(ex.Message);
            }

            try
            {
                SyncFinished?.Invoke(outcome);
            }
            catch
            {
                // Assinantes não devem rebentar o pipeline de sync.
            }

            return outcome;
        }
        finally
        {
            _syncRunLock.Release();
        }
    }

    /// <summary>
    /// Só contacta a API quando <paramref name="force"/> é <c>true</c> (botão ⟳ / «Sincronizar»).
    /// Caso contrário devolve <see cref="GradesSyncStatus.SkippedTooSoon"/> e mantém dados locais.
    /// </summary>
    private async Task<GradesSyncOutcome> RunInternalAsync(bool force, CancellationToken cancellationToken)
    {
        if (!force)
        {
            return GradesSyncOutcome.TooSoon();
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return GradesSyncOutcome.NoNetwork();
        }

        var status = await _api.GetGradesSyncStatusAsync(cancellationToken).ConfigureAwait(false);
        if (status?.GradesVersion == null)
        {
            return GradesSyncOutcome.Failed("Não foi possível verificar atualizações.");
        }

        var previousVersion = Preferences.Default.Get(OlondongePreferenceKeys.SavedGradesVersion, string.Empty);
        var requireCurrentYearRefresh = await NeedsCurrentYearRefreshAsync(cancellationToken).ConfigureAwait(false);
        if (!requireCurrentYearRefresh && !string.IsNullOrEmpty(previousVersion) && previousVersion == status.GradesVersion)
        {
            await PersistStudentAppPayloadsAsync(cancellationToken).ConfigureAwait(false);
            Preferences.Default.Set(OlondongePreferenceKeys.LastSyncPipelineTicks, DateTime.UtcNow.Ticks);
            return GradesSyncOutcome.NoChange();
        }

        var enrolments = NormalizeCurrentYearEnrolments(
            await _api.GetEnrolmentsAsync(cancellationToken).ConfigureAwait(false));
        if (enrolments.Count == 0)
        {
            await _store.ReplaceEnrolmentsAsync(Array.Empty<EnrolmentDto>(), cancellationToken).ConfigureAwait(false);
            await _store.SetMetaAsync(CurrentYearSyncMarkerKey, "1", cancellationToken).ConfigureAwait(false);
            Preferences.Default.Set(OlondongePreferenceKeys.SavedGradesVersion, status.GradesVersion);
            Preferences.Default.Set(OlondongePreferenceKeys.LastSyncPipelineTicks, DateTime.UtcNow.Ticks);
            await _store.SetMetaAsync("last_full_sync_utc", DateTime.UtcNow.ToString("o"), cancellationToken).ConfigureAwait(false);
            await PersistStudentAppPayloadsAsync(cancellationToken).ConfigureAwait(false);
            return GradesSyncOutcome.Success(false);
        }

        await _store.ReplaceEnrolmentsAsync(enrolments.ToList(), cancellationToken).ConfigureAwait(false);
        Preferences.Default.Set(OlondongePreferenceKeys.EnrolmentsApiContractVersion, RequiredEnrolmentsApiContractVersion);
        await _store.SetMetaAsync(CurrentYearSyncMarkerKey, "1", cancellationToken).ConfigureAwait(false);

        const int maxParallel = 1;
        using var enrolGate = new SemaphoreSlim(maxParallel, maxParallel);
        var enrolTasks = enrolments.Select(e => SyncOneEnrolmentAsync(e, enrolGate, cancellationToken)).ToArray();
        var perEnrolResults = await Task.WhenAll(enrolTasks).ConfigureAwait(false);
        var allEnrolmentsOk = perEnrolResults.All(r => r);
        var failedCount = perEnrolResults.Count(r => !r);

        await PersistStudentAppPayloadsAsync(cancellationToken).ConfigureAwait(false);

        // Cada corrida actualiza sempre o LastSyncPipelineTicks (tentativa registada).
        Preferences.Default.Set(OlondongePreferenceKeys.LastSyncPipelineTicks, DateTime.UtcNow.Ticks);
        Preferences.Default.Set(OlondongePreferenceKeys.LastEnrolmentsRefreshTicks, DateTime.UtcNow.Ticks);

        if (allEnrolmentsOk)
        {
            // Só consideramos "totalmente sincronizado" quando nenhuma matrícula falhou.
            // Caso contrário, mantemos a SavedGradesVersion antiga para que a próxima sync (force ou agendada)
            // ainda detecte previousVersion != server e volte a tentar.
            await _store.SetMetaAsync("last_full_sync_utc", DateTime.UtcNow.ToString("o"), cancellationToken).ConfigureAwait(false);
            Preferences.Default.Set(OlondongePreferenceKeys.SavedGradesVersion, status.GradesVersion);
        }
        else
        {
            _logger.LogWarning(
                "Sync parcial: {Failed}/{Total} matrículas falharam. SavedGradesVersion mantida para forçar retry na próxima corrida.",
                failedCount,
                perEnrolResults.Length);
        }

        var dataChanged = !string.IsNullOrEmpty(previousVersion) && previousVersion != status.GradesVersion;
        if (dataChanged && allEnrolmentsOk)
        {
            await _notifier.NotifyGradesUpdatedAsync(cancellationToken).ConfigureAwait(false);
        }

        return GradesSyncOutcome.Success(dataChanged);
    }

    /// <summary>Horário, currículo e propinas — última cópia em <c>sync_meta</c> para páginas offline.</summary>
    private async Task PersistStudentAppPayloadsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _store.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            var enrolments = (await _store.GetEnrolmentsAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var mid = PickPrimaryMatriculaId(enrolments);
            if (mid <= 0)
            {
                return;
            }

            var schedule = await _api.GetWeeklyScheduleAsync(mid, cancellationToken).ConfigureAwait(false);
            if (schedule != null)
            {
                await _store.SetMetaAsync(
                    StudentPayloadCacheKeys.WeeklyScheduleJson,
                    JsonSerializer.Serialize(schedule, AppJson.Options),
                    cancellationToken).ConfigureAwait(false);
            }

            var curriculum = await _api.GetCurriculumAsync(mid, cancellationToken).ConfigureAwait(false);
            curriculum = StudentGradesApi.NormalizeCurriculumResponse(curriculum);
            if (curriculum != null)
            {
                await _store.SetMetaAsync(
                    StudentPayloadCacheKeys.CurriculumJson,
                    JsonSerializer.Serialize(curriculum, AppJson.Options),
                    cancellationToken).ConfigureAwait(false);
            }

            var fees = await _api.GetFeesAsync(mid, cancellationToken).ConfigureAwait(false);
            if (fees != null)
            {
                await _store.SetMetaAsync(
                    StudentPayloadCacheKeys.FeesJson,
                    JsonSerializer.Serialize(fees, AppJson.Options),
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache local de horário/currículo/propinas: falha parcial ao persistir");
        }
    }

    private static int PickPrimaryMatriculaId(IReadOnlyList<EnrolmentDto> enrolments)
    {
        if (enrolments.Count == 0)
        {
            return 0;
        }

        var current = enrolments.Where(static x => x.IsCurrentAnoLectivo).ToList();
        if (current.Count > 0)
        {
            return current.OrderByDescending(static x => x.MatriculaId).First().MatriculaId;
        }

        return enrolments.OrderByDescending(static x => x.MatriculaId).First().MatriculaId;
    }

    private async Task<bool> NeedsCurrentYearRefreshAsync(CancellationToken cancellationToken)
    {
        var localContract = Preferences.Default.Get(OlondongePreferenceKeys.EnrolmentsApiContractVersion, 0);
        if (localContract < RequiredEnrolmentsApiContractVersion)
        {
            return true;
        }

        var marker = await _store.GetMetaAsync(CurrentYearSyncMarkerKey, cancellationToken).ConfigureAwait(false);
        if (marker == "1")
        {
            return false;
        }

        var localEnrolments = await _store.GetEnrolmentsAsync(cancellationToken).ConfigureAwait(false);
        return localEnrolments.Count > 0;
    }

    private static bool AreEnrolmentsEquivalent(IReadOnlyList<EnrolmentDto> local, IReadOnlyList<EnrolmentDto> remote)
    {
        if (local.Count != remote.Count)
        {
            return false;
        }

        var byId = local.ToDictionary(x => x.MatriculaId);
        foreach (var remoteEnrolment in remote)
        {
            if (!byId.TryGetValue(remoteEnrolment.MatriculaId, out var localEnrolment))
            {
                return false;
            }

            if (localEnrolment.AnoLectivoId != remoteEnrolment.AnoLectivoId
                || localEnrolment.CursoId != remoteEnrolment.CursoId
                || localEnrolment.IsCurrentAnoLectivo != remoteEnrolment.IsCurrentAnoLectivo
                || !string.Equals(localEnrolment.AnoLectivoName, remoteEnrolment.AnoLectivoName, StringComparison.Ordinal)
                || !string.Equals(localEnrolment.ClasseName, remoteEnrolment.ClasseName, StringComparison.Ordinal)
                || !string.Equals(localEnrolment.CursoName, remoteEnrolment.CursoName, StringComparison.Ordinal)
                || !string.Equals(localEnrolment.CursoAbreviatura, remoteEnrolment.CursoAbreviatura, StringComparison.Ordinal)
                || !string.Equals(localEnrolment.TurnoName, remoteEnrolment.TurnoName, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<EnrolmentDto> NormalizeCurrentYearEnrolments(IReadOnlyList<EnrolmentDto> enrolments)
    {
        if (enrolments.Count == 0 || enrolments.Any(x => x.IsCurrentAnoLectivo))
        {
            return enrolments;
        }

        var currentAnoLectivoId = enrolments
            .Where(x => x.AnoLectivoId > 0)
            .Select(x => x.AnoLectivoId)
            .DefaultIfEmpty(0)
            .Max();

        if (currentAnoLectivoId > 0)
        {
            foreach (var enrolment in enrolments)
            {
                enrolment.IsCurrentAnoLectivo = enrolment.AnoLectivoId == currentAnoLectivoId;
            }
        }
        else
        {
            var latestMatriculaId = enrolments.Max(x => x.MatriculaId);
            foreach (var enrolment in enrolments)
            {
                enrolment.IsCurrentAnoLectivo = enrolment.MatriculaId == latestMatriculaId;
            }
        }

        return enrolments;
    }

    /// <returns><c>true</c> se a matrícula sincronizou completamente; <c>false</c> em caso de qualquer falha (será re-tentada na próxima corrida).</returns>
    private async Task<bool> SyncOneEnrolmentAsync(
        EnrolmentDto e,
        SemaphoreSlim gate,
        CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            try
            {
                var allLines = await _api.GetGradesAsync(e.MatriculaId, null, includeExams: false, cancellationToken).ConfigureAwait(false);
                var historySummary = await _api.GetHistorySummaryAsync(e.MatriculaId, cancellationToken).ConfigureAwait(false);

                var distinct = allLines
                    .GroupBy(g => g.PeriodoId)
                    .Select(g => (g.Key, g.First().PeriodoName))
                    .OrderBy(x => x.Key)
                    .ToList();

                await _store.ReplacePeriodsAsync(e.MatriculaId, distinct, cancellationToken).ConfigureAwait(false);

                foreach (var periodGroup in allLines.GroupBy(x => x.PeriodoId).OrderBy(g => g.Key))
                {
                    var list = (IReadOnlyList<GradeLineDto>)periodGroup.ToList();
                    await _store.ReplaceGradesAsync(e.MatriculaId, periodGroup.Key, list, cancellationToken).ConfigureAwait(false);
                }

                if (historySummary != null)
                {
                    await _store.UpsertHistorySummaryAsync(historySummary, cancellationToken).ConfigureAwait(false);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sync matrícula {MatriculaId}: ignorar esta matrícula e continuar", e.MatriculaId);
                return false;
            }
        }
        finally
        {
            gate.Release();
        }
    }
}
