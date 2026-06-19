using OlondongeApp.Models.Dtos;

namespace OlondongeApp.Services;

public interface IGradesLocalStore
{
    Task EnsureReadyAsync(CancellationToken cancellationToken = default);

    Task ReplaceEnrolmentsAsync(IReadOnlyList<EnrolmentDto> enrolments, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrolmentDto>> GetEnrolmentsAsync(CancellationToken cancellationToken = default);

    Task SetMetaAsync(string key, string value, CancellationToken cancellationToken = default);

    Task<string?> GetMetaAsync(string key, CancellationToken cancellationToken = default);

    Task ReplacePeriodsAsync(int matriculaId, IReadOnlyList<(int PeriodoId, string? Name)> periods, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(int PeriodoId, string? Name)>> GetPeriodsAsync(int matriculaId, CancellationToken cancellationToken = default);

    Task ReplaceGradesAsync(int matriculaId, int periodoId, IReadOnlyList<GradeLineDto> grades, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GradeLineDto>> GetGradesAsync(int matriculaId, int periodoId, CancellationToken cancellationToken = default);

    /// <summary>Lê todas as linhas de notas das matrículas indicadas (um acesso ao SQLite).</summary>
    Task<IReadOnlyList<GradeLineDto>> GetGradesForMatriculasAsync(IReadOnlyList<int> matriculaIds, CancellationToken cancellationToken = default);

    Task UpsertHistorySummaryAsync(HistorySummaryDto summary, CancellationToken cancellationToken = default);

    Task<HistorySummaryDto?> GetHistorySummaryAsync(int matriculaId, CancellationToken cancellationToken = default);

    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
