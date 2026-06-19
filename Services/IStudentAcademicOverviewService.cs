namespace OlondongeApp.Services;

/// <summary>
/// Resumo académico a partir dos dados já sincronizados localmente (notas / matrículas).
/// </summary>
public interface IStudentAcademicOverviewService
{
    /// <summary>
    /// Indicadores (KPI) para o painel inicial, a partir do cache local.
    /// </summary>
    Task<StudentDashboardInsights> GetDashboardInsightsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disciplinas em notas sincronizadas apenas para matrículas do ano lectivo actual (<c>is_current_ano_lectivo</c>).
    /// </summary>
    Task<IReadOnlyList<string>> GetDisciplineNamesForCurrentSchoolYearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disciplinas em notas sincronizadas para todas as matrículas do mesmo curso que a matrícula de referência
    /// (prioridade: ano lectivo actual; caso contrário o ano mais recente), alinhado à grelha oficial por <c>curso_id</c>.
    /// </summary>
    Task<IReadOnlyList<string>> GetDisciplineNamesForCourseFromSyncedGradesAsync(CancellationToken cancellationToken = default);

    /// <summary>Texto para horário: abreviatura se existir em cache, senão nome (ano lectivo actual).</summary>
    Task<IReadOnlyList<string>> GetDisciplineScheduleDisplayTextsCurrentYearAsync(CancellationToken cancellationToken = default);

    /// <summary>Texto para horário: abreviatura se existir em cache, senão nome (mesmo critério de curso que <see cref="GetDisciplineNamesForCourseFromSyncedGradesAsync"/>).</summary>
    Task<IReadOnlyList<string>> GetDisciplineScheduleDisplayTextsForCourseFromSyncedGradesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Minutos desde a meia-noite para o 1.º tempo do horário estimado (manhã 07:15, tarde 13:00), a partir do turno da matrícula do ano lectivo actual em cache.
    /// </summary>
    Task<int> GetScheduleFallbackDayStartMinutesAsync(CancellationToken cancellationToken = default);
}

public sealed class StudentDashboardInsights
{
    public bool HasMatriculaAnoActual { get; init; }

    /// <summary>Classe da matrícula do ano lectivo actual (mesma prioridade que Notas).</summary>
    public string? MatriculaAnoActualClasseName { get; init; }

    /// <summary>Designação do ano lectivo actual em cache.</summary>
    public string? MatriculaAnoActualAnoLectivoName { get; init; }

    /// <summary>
    /// Número de disciplinas do ano lectivo actual com nota principal (última avaliacao_cab em cache) abaixo de 10.
    /// </summary>
    public int NegativasDisciplinasCount { get; init; }

    /// <summary>Ranking da turma (resumo em cache) da matrícula principal do ano lectivo actual.</summary>
    public int? RankingTurmaPosicao { get; init; }

    public int? RankingTurmaTotal { get; init; }

    public bool HasRankingData =>
        RankingTurmaPosicao is > 0 && RankingTurmaTotal is > 0;
}
