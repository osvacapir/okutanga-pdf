using Microsoft.Extensions.Logging;
using OlondongeApp.Models.Dtos;

namespace OlondongeApp.Services;

public sealed class StudentAcademicOverviewService : IStudentAcademicOverviewService
{
    private const decimal PassThreshold = 10m;

    private readonly IGradesLocalStore _store;
    private readonly ILogger<StudentAcademicOverviewService> _logger;

    public StudentAcademicOverviewService(IGradesLocalStore store, ILogger<StudentAcademicOverviewService> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task<StudentDashboardInsights> GetDashboardInsightsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetDashboardInsightsCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Indicadores do painel: falha ao ler cache local (SQLite ou outro)");

            return new StudentDashboardInsights();
        }
    }

    private async Task<StudentDashboardInsights> GetDashboardInsightsCoreAsync(CancellationToken cancellationToken)
    {
        await _store.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

        var enrolments = (await _store.GetEnrolmentsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        var currentYear = enrolments.Where(e => e.IsCurrentAnoLectivo).ToList();
        var principalAnoActual = GetMatriculaAnoLectivoActualEnrolment(currentYear);

        var matriculaIds = currentYear.Select(e => e.MatriculaId).Distinct().ToList();
        var currentMatriculaSet = new HashSet<int>(matriculaIds);
        var allGrades = matriculaIds.Count == 0
            ? Array.Empty<GradeLineDto>()
            : await _store.GetGradesForMatriculasAsync(matriculaIds, cancellationToken).ConfigureAwait(false);

        // Por disciplina (ano actual): linha da última avaliacao_cab (maior avaliacao_id) em cache.
        var latestRowByMatriculaDisc = new Dictionary<(int MatriculaId, string DiscKey), (GradeLineDto Row, int PeriodoId)>();
        foreach (var g in allGrades)
        {
            var discKey = DisciplineKey(g);
            if (string.IsNullOrWhiteSpace(discKey))
            {
                continue;
            }

            var mid = g.MatriculaId;
            if (!currentMatriculaSet.Contains(mid))
            {
                continue;
            }

            var periodoId = g.PeriodoId;
            var key = (mid, discKey);
            if (!latestRowByMatriculaDisc.TryGetValue(key, out var prev))
            {
                latestRowByMatriculaDisc[key] = (g, periodoId);
                continue;
            }

            if (g.AvaliacaoId > prev.Row.AvaliacaoId)
            {
                latestRowByMatriculaDisc[key] = (g, periodoId);
            }
            else if (g.AvaliacaoId == prev.Row.AvaliacaoId && periodoId > prev.PeriodoId)
            {
                latestRowByMatriculaDisc[key] = (g, periodoId);
            }
        }

        var negativasCount = latestRowByMatriculaDisc
            .Select(x => GetPrincipalDetScore(x.Value.Row))
            .Count(score => score.HasValue && score.Value < PassThreshold);

        int? rankingPos = null;
        int? rankingTotal = null;
        var matriculaAnoActualId = principalAnoActual?.MatriculaId;
        if (matriculaAnoActualId.HasValue)
        {
            var summary = await _store.GetHistorySummaryAsync(matriculaAnoActualId.Value, cancellationToken).ConfigureAwait(false);
            if (summary?.RankingPosicao is > 0 && summary.RankingTotal is > 0)
            {
                rankingPos = summary.RankingPosicao;
                rankingTotal = summary.RankingTotal;
            }
        }

        return new StudentDashboardInsights
        {
            HasMatriculaAnoActual = principalAnoActual is not null,
            MatriculaAnoActualClasseName = principalAnoActual?.ClasseName?.Trim(),
            MatriculaAnoActualAnoLectivoName = principalAnoActual?.AnoLectivoName?.Trim(),
            NegativasDisciplinasCount = negativasCount,
            RankingTurmaPosicao = rankingPos,
            RankingTurmaTotal = rankingTotal,
        };
    }

    public async Task<IReadOnlyList<string>> GetDisciplineNamesForCurrentSchoolYearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetDisciplineNamesForCurrentSchoolYearCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Disciplinas (ano actual): falha ao ler cache");

            return Array.Empty<string>();
        }
    }

    private async Task<IReadOnlyList<string>> GetDisciplineNamesForCurrentSchoolYearCoreAsync(CancellationToken cancellationToken)
    {
        await _store.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var enrolments = await _store.GetEnrolmentsAsync(cancellationToken).ConfigureAwait(false);
        var ids = enrolments.Where(x => x.IsCurrentAnoLectivo).Select(x => x.MatriculaId).Distinct().ToList();
        var allGrades = ids.Count == 0
            ? Array.Empty<GradeLineDto>()
            : await _store.GetGradesForMatriculasAsync(ids, cancellationToken).ConfigureAwait(false);
        foreach (var g in allGrades)
        {
            var label = !string.IsNullOrWhiteSpace(g.DisciplinaName)
                ? g.DisciplinaName!.Trim()
                : g.DisciplinaAbreviatura?.Trim();
            if (!string.IsNullOrEmpty(label))
            {
                names.Add(label);
            }
        }

        return names.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public async Task<IReadOnlyList<string>> GetDisciplineNamesForCourseFromSyncedGradesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetDisciplineNamesForCourseFromSyncedGradesCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Disciplinas por curso (cache): falha ao ler SQLite");

            return Array.Empty<string>();
        }
    }

    private async Task<IReadOnlyList<string>> GetDisciplineNamesForCourseFromSyncedGradesCoreAsync(CancellationToken cancellationToken)
    {
        await _store.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
        var enrolments = (await _store.GetEnrolmentsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        if (enrolments.Count == 0)
        {
            return Array.Empty<string>();
        }

        var reference = GetPrimaryEnrolmentForCourseGrouping(enrolments);
        if (reference is null)
        {
            return Array.Empty<string>();
        }

        var sameCourseMatriculaIds = EnrolmentsInSameCourseAs(reference, enrolments)
            .Select(e => e.MatriculaId)
            .Distinct()
            .ToList();

        var allGrades = sameCourseMatriculaIds.Count == 0
            ? Array.Empty<GradeLineDto>()
            : await _store.GetGradesForMatriculasAsync(sameCourseMatriculaIds, cancellationToken).ConfigureAwait(false);

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in allGrades)
        {
            var label = !string.IsNullOrWhiteSpace(g.DisciplinaName)
                ? g.DisciplinaName!.Trim()
                : g.DisciplinaAbreviatura?.Trim();
            if (!string.IsNullOrEmpty(label))
            {
                names.Add(label);
            }
        }

        return names.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public async Task<IReadOnlyList<string>> GetDisciplineScheduleDisplayTextsCurrentYearAsync(CancellationToken cancellationToken = default)
    {
        await _store.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
        var enrolments = await _store.GetEnrolmentsAsync(cancellationToken).ConfigureAwait(false);
        var ids = enrolments.Where(x => x.IsCurrentAnoLectivo).Select(x => x.MatriculaId).Distinct().ToList();
        var allGrades = ids.Count == 0
            ? Array.Empty<GradeLineDto>()
            : await _store.GetGradesForMatriculasAsync(ids, cancellationToken).ConfigureAwait(false);

        return BuildScheduleDisplayTextsFromGrades(allGrades);
    }

    public async Task<IReadOnlyList<string>> GetDisciplineScheduleDisplayTextsForCourseFromSyncedGradesAsync(CancellationToken cancellationToken = default)
    {
        await _store.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
        var enrolments = (await _store.GetEnrolmentsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        if (enrolments.Count == 0)
        {
            return Array.Empty<string>();
        }

        var reference = GetPrimaryEnrolmentForCourseGrouping(enrolments);
        if (reference is null)
        {
            return Array.Empty<string>();
        }

        var sameCourseMatriculaIds = EnrolmentsInSameCourseAs(reference, enrolments)
            .Select(e => e.MatriculaId)
            .Distinct()
            .ToList();

        var allGrades = sameCourseMatriculaIds.Count == 0
            ? Array.Empty<GradeLineDto>()
            : await _store.GetGradesForMatriculasAsync(sameCourseMatriculaIds, cancellationToken).ConfigureAwait(false);

        return BuildScheduleDisplayTextsFromGrades(allGrades);
    }

    public async Task<int> GetScheduleFallbackDayStartMinutesAsync(CancellationToken cancellationToken = default)
    {
        await _store.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
        var enrolments = (await _store.GetEnrolmentsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        if (enrolments.Count == 0)
        {
            return MapTurnoNameToFirstLessonStartMinutes(null);
        }

        var currentYear = enrolments.Where(e => e.IsCurrentAnoLectivo).ToList();
        var principal = currentYear.Count > 0
            ? GetMatriculaAnoLectivoActualEnrolment(currentYear)
            : GetPrimaryEnrolmentForCourseGrouping(enrolments);

        return MapTurnoNameToFirstLessonStartMinutes(principal?.TurnoName);
    }

    /// <summary>1.º tempo: tarde 13:00; caso contrário (manhã ou desconhecido) 07:15.</summary>
    public static int MapTurnoNameToFirstLessonStartMinutes(string? turnoName)
    {
        var t = turnoName?.Trim();
        if (string.IsNullOrEmpty(t))
        {
            return MorningFirstLessonMinutesFromMidnight;
        }

        var lower = t.ToLowerInvariant();
        if (lower.Contains("tarde", StringComparison.Ordinal) || lower.Contains("vespert", StringComparison.Ordinal))
        {
            return AfternoonFirstLessonMinutesFromMidnight;
        }

        return MorningFirstLessonMinutesFromMidnight;
    }

    private const int MorningFirstLessonMinutesFromMidnight = 7 * 60 + 15;
    private const int AfternoonFirstLessonMinutesFromMidnight = 13 * 60;

    /// <summary>Para o horário: preferir <see cref="GradeLineDto.DisciplinaAbreviatura"/> quando preenchida.</summary>
    private static IReadOnlyList<string> BuildScheduleDisplayTextsFromGrades(IEnumerable<GradeLineDto> grades)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in grades)
        {
            var text = !string.IsNullOrWhiteSpace(g.DisciplinaAbreviatura)
                ? g.DisciplinaAbreviatura.Trim()
                : g.DisciplinaName?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                set.Add(text);
            }
        }

        return set.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    /// <summary>
    /// Matrícula preferida para agrupar o curso (mesmo critério de prioridade que o painel: ano actual, maior <c>matricula_id</c>).
    /// </summary>
    private static EnrolmentDto? GetPrimaryEnrolmentForCourseGrouping(IReadOnlyList<EnrolmentDto> enrolments)
    {
        var currentYear = enrolments.Where(e => e.IsCurrentAnoLectivo).ToList();
        if (currentYear.Count > 0)
        {
            return GetMatriculaAnoLectivoActualEnrolment(currentYear);
        }

        var maxAno = enrolments.Where(e => e.AnoLectivoId > 0).Select(e => e.AnoLectivoId).DefaultIfEmpty(0).Max();
        if (maxAno <= 0)
        {
            return enrolments.OrderByDescending(e => e.MatriculaId).First();
        }

        var inLatestAno = enrolments.Where(e => e.AnoLectivoId == maxAno).ToList();

        return inLatestAno.Count > 0
            ? inLatestAno.OrderByDescending(e => e.MatriculaId).First()
            : enrolments.OrderByDescending(e => e.MatriculaId).First();
    }

    private static IEnumerable<EnrolmentDto> EnrolmentsInSameCourseAs(EnrolmentDto reference, IReadOnlyList<EnrolmentDto> enrolments)
    {
        if (reference.CursoId > 0)
        {
            foreach (var e in enrolments)
            {
                if (e.CursoId == reference.CursoId)
                {
                    yield return e;
                }
                else if (e.CursoId == 0 && CoursesMatchByName(reference, e))
                {
                    yield return e;
                }
            }

            yield break;
        }

        foreach (var e in enrolments)
        {
            if (CoursesMatchByName(reference, e))
            {
                yield return e;
            }
        }
    }

    private static bool CoursesMatchByName(EnrolmentDto a, EnrolmentDto b)
    {
        var na = NormalizeCursoName(a.CursoName);
        var nb = NormalizeCursoName(b.CursoName);

        return !string.IsNullOrEmpty(na) && string.Equals(na, nb, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeCursoName(string? name)
    {
        var t = name?.Trim();

        return string.IsNullOrEmpty(t) ? null : t;
    }

    /// <summary>
    /// Nota principal do detalhe (avaliacao_det), alinhada à API: MT, depois CPT, CPP se activo, MAC.
    /// </summary>
    private static decimal? GetPrincipalDetScore(GradeLineDto g)
    {
        if (g.Mt.HasValue)
        {
            return g.Mt.Value;
        }

        if (g.Cpt.HasValue)
        {
            return g.Cpt.Value;
        }

        if (g.CppEnabled && g.Cpp.HasValue)
        {
            return g.Cpp.Value;
        }

        if (g.Mac.HasValue)
        {
            return g.Mac.Value;
        }

        return null;
    }

    private static string DisciplineKey(GradeLineDto g)
    {
        var name = g.DisciplinaName?.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        return g.DisciplinaAbreviatura?.Trim() ?? $"id:{g.AvaliacaoId}";
    }

    /// <summary>
    /// Matrícula do ano lectivo actual: entradas com <see cref="EnrolmentDto.IsCurrentAnoLectivo"/>.
    /// Se existir mais do que uma, usa a mesma prioridade que a página Notas (maior <c>matricula_id</c> primeiro).
    /// </summary>
    private static EnrolmentDto? GetMatriculaAnoLectivoActualEnrolment(IReadOnlyList<EnrolmentDto> currentYear)
    {
        if (currentYear.Count == 0)
        {
            return null;
        }

        return currentYear.OrderByDescending(e => e.MatriculaId).First();
    }
}
