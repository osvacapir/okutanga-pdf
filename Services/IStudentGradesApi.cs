using OlondongeApp.Models.Dtos;

namespace OlondongeApp.Services;

public interface IStudentGradesApi
{
    Task<GradesSyncStatusDto?> GetGradesSyncStatusAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrolmentDto>> GetEnrolmentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PeriodRowDto>> GetGradePeriodsAsync(int matriculaId, CancellationToken cancellationToken = default);

    /// <param name="periodoId">Quando null, devolve todos os períodos (GET sem query periodo_id).</param>
    /// <param name="includeExams">Quando false, pede ao backend para omitir exames (payload menor).</param>
    Task<IReadOnlyList<GradeLineDto>> GetGradesAsync(int matriculaId, int? periodoId = null, bool includeExams = true, CancellationToken cancellationToken = default);

    Task<HistorySummaryDto?> GetHistorySummaryAsync(int matriculaId, CancellationToken cancellationToken = default);

    Task<StudentCurriculumResponseDto?> GetCurriculumAsync(int? matriculaId = null, CancellationToken cancellationToken = default);

    /// <summary>Grelha curricular a partir do curso (sem matrícula na URL); resposta inclui <c>classes[]</c> agrupadas.</summary>
    Task<StudentCurriculumResponseDto?> GetCurriculumByCourseAsync(int cursoId, CancellationToken cancellationToken = default);

    Task<StudentWeeklyScheduleResponseDto?> GetWeeklyScheduleAsync(int? matriculaId = null, CancellationToken cancellationToken = default);

    Task<StudentFeesResponseDto?> GetFeesAsync(int? matriculaId = null, CancellationToken cancellationToken = default);

    Task<DownloadFileResult?> DownloadBoletimAsync(int matriculaId, int periodoId, CancellationToken cancellationToken = default);

    Task<DownloadFileResult?> DownloadFichaAcademicaAsync(int matriculaId, CancellationToken cancellationToken = default);
}
