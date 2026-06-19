using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OlondongeApp.Models.Dtos;

namespace OlondongeApp.Services;

public sealed class StudentGradesApi : IStudentGradesApi
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StudentGradesApi> _logger;

    public StudentGradesApi(HttpClient httpClient, ILogger<StudentGradesApi> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Ligação HTTP ou leitura do corpo podem falhar (502, reset, resposta prematura) sem status HTTP útil.
    /// </summary>
    private async Task<(bool Ok, string Body, HttpStatusCode? StatusCode)> TryGetJsonBodyAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return (response.IsSuccessStatusCode, body, response.StatusCode);
        }
        catch (OperationCanceledException)
        {
            // Timeout ou cancelamento do HttpClient (navegação); não propagar para evitar crash da página.
            return (false, string.Empty, null);
        }
        catch (HttpRequestException)
        {
            return (false, string.Empty, null);
        }
        catch
        {
            return (false, string.Empty, null);
        }
    }

    private async Task<DownloadFileResult?> TryDownloadAsync(string relativeUrl, string fallbackFileName, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? fallbackFileName;
            fileName = fileName.Trim('"');

            return new DownloadFileResult
            {
                Content = bytes,
                FileName = fileName,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf",
            };
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<GradesSyncStatusDto?> GetGradesSyncStatusAsync(CancellationToken cancellationToken = default)
    {
        var (ok, body, status) = await TryGetJsonBodyAsync("student/grades/sync-status", cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            _logger.LogWarning("GET grades/sync-status falhou Status={StatusCode}", status.HasValue ? (int)status.Value : null);
            return null;
        }

        var wrap = JsonSerializer.Deserialize<ApiResponse<GradesSyncStatusDto>>(body, AppJson.Options);
        return wrap?.Data;
    }

    public async Task<IReadOnlyList<EnrolmentDto>> GetEnrolmentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (ok, body, status) = await TryGetJsonBodyAsync("student/enrolments", cancellationToken).ConfigureAwait(false);
            if (!ok)
            {
                _logger.LogWarning("GET student/enrolments falhou Status={StatusCode}", status.HasValue ? (int)status.Value : null);
                return Array.Empty<EnrolmentDto>();
            }

            var wrap = JsonSerializer.Deserialize<ApiResponse<List<EnrolmentDto>>>(body, AppJson.Options);
            var list = wrap?.Data ?? new List<EnrolmentDto>();
            MergeEnrolmentFieldsFromJsonBody(body, list);

            return list;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "GET student/enrolments: resposta não é JSON válido");

            return Array.Empty<EnrolmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GET student/enrolments: erro inesperado");

            return Array.Empty<EnrolmentDto>();
        }
    }

    /// <summary>
    /// Preenche campos em falha de deserialização (ex.: chaves camelCase vs snake_case, proxies).
    /// </summary>
    private static void MergeEnrolmentFieldsFromJsonBody(string json, List<EnrolmentDto> list)
    {
        if (list.Count == 0 || string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            var index = 0;
            foreach (var row in data.EnumerateArray())
            {
                if (index >= list.Count)
                {
                    break;
                }

                var e = list[index++];
                var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in row.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var s = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            map[prop.Name] = s.Trim();
                        }
                    }
                }

                static string? Pick(Dictionary<string, string?> m, params string[] keys)
                {
                    foreach (var k in keys)
                    {
                        if (m.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                        {
                            return v;
                        }
                    }

                    return null;
                }

                if (string.IsNullOrWhiteSpace(e.ClasseName))
                {
                    e.ClasseName = Pick(map, "classe_name", "classeName", "ClasseName");
                }

                if (string.IsNullOrWhiteSpace(e.CursoName))
                {
                    e.CursoName = Pick(map, "curso_name", "cursoName", "CursoName");
                }

                if (string.IsNullOrWhiteSpace(e.CursoAbreviatura))
                {
                    e.CursoAbreviatura = Pick(map, "curso_abreviatura", "cursoAbreviatura", "CursoAbreviatura");
                }
            }
        }
        catch
        {
            // merge best-effort
        }
    }

    public async Task<IReadOnlyList<PeriodRowDto>> GetGradePeriodsAsync(int matriculaId, CancellationToken cancellationToken = default)
    {
        var url = $"student/grades/periods?matricula_id={matriculaId}";
        var (ok, body, status) = await TryGetJsonBodyAsync(url, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            _logger.LogWarning("GET grades/periods falhou Status={StatusCode} MatriculaId={MatriculaId}", status.HasValue ? (int)status.Value : null, matriculaId);
            return Array.Empty<PeriodRowDto>();
        }

        var wrap = JsonSerializer.Deserialize<ApiResponse<List<PeriodRowDto>>>(body, AppJson.Options);
        return wrap?.Data ?? new List<PeriodRowDto>();
    }

    public async Task<IReadOnlyList<GradeLineDto>> GetGradesAsync(int matriculaId, int? periodoId = null, bool includeExams = true, CancellationToken cancellationToken = default)
    {
        var url = periodoId.HasValue
            ? $"student/grades?matricula_id={matriculaId}&periodo_id={periodoId.Value}"
            : $"student/grades?matricula_id={matriculaId}";
        if (!includeExams)
        {
            url += "&include_exams=0";
        }
        var (ok, body, status) = await TryGetJsonBodyAsync(url, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            _logger.LogWarning("GET student/grades falhou Status={StatusCode} MatriculaId={MatriculaId} PeriodoId={PeriodoId} IncludeExams={IncludeExams}", status.HasValue ? (int)status.Value : null, matriculaId, periodoId, includeExams);
            return Array.Empty<GradeLineDto>();
        }

        var wrap = JsonSerializer.Deserialize<ApiResponse<List<GradeLineDto>>>(body, AppJson.Options);
        return wrap?.Data ?? new List<GradeLineDto>();
    }

    public async Task<HistorySummaryDto?> GetHistorySummaryAsync(int matriculaId, CancellationToken cancellationToken = default)
    {
        var url = $"student/history/summary?matricula_id={matriculaId}";
        var (ok, body, status) = await TryGetJsonBodyAsync(url, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            _logger.LogWarning("GET history/summary falhou Status={StatusCode} MatriculaId={MatriculaId}", status.HasValue ? (int)status.Value : null, matriculaId);
            return null;
        }

        var wrap = JsonSerializer.Deserialize<ApiResponse<HistorySummaryDto>>(body, AppJson.Options);
        return wrap?.Data;
    }

    public async Task<StudentCurriculumResponseDto?> GetCurriculumAsync(int? matriculaId = null, CancellationToken cancellationToken = default)
    {
        var url = matriculaId.HasValue ? $"student/curriculum?matricula_id={matriculaId.Value}" : "student/curriculum";
        var (ok, body, status) = await TryGetJsonBodyAsync(url, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            _logger.LogWarning("GET student/curriculum falhou Status={StatusCode} MatriculaId={MatriculaId}", status.HasValue ? (int)status.Value : null, matriculaId);
            return null;
        }

        try
        {
            var wrap = JsonSerializer.Deserialize<ApiResponse<StudentCurriculumResponseDto>>(body, AppJson.Options);

            return NormalizeCurriculumResponse(wrap?.Data);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "GET student/curriculum: corpo JSON inválido MatriculaId={MatriculaId}", matriculaId);

            return null;
        }
    }

    public async Task<StudentCurriculumResponseDto?> GetCurriculumByCourseAsync(int cursoId, CancellationToken cancellationToken = default)
    {
        var url = $"student/curriculum/by-course?curso_id={cursoId}";
        var (ok, body, status) = await TryGetJsonBodyAsync(url, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            _logger.LogWarning("GET student/curriculum/by-course falhou Status={StatusCode} CursoId={CursoId}", status.HasValue ? (int)status.Value : null, cursoId);
            return null;
        }

        try
        {
            var wrap = JsonSerializer.Deserialize<ApiResponse<StudentCurriculumResponseDto>>(body, AppJson.Options);

            return NormalizeCurriculumResponse(wrap?.Data);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "GET student/curriculum/by-course: corpo JSON inválido CursoId={CursoId}", cursoId);

            return null;
        }
    }

    /// <summary>Garante listas não nulas após JSON com <c>"classes": null</c> ou disciplinas em falta.</summary>
    public static StudentCurriculumResponseDto? NormalizeCurriculumResponse(StudentCurriculumResponseDto? d)
    {
        if (d == null)
        {
            return null;
        }

        d.Classes ??= new List<CurriculumClasseGroupDto>();
        d.Items ??= new List<CurriculumItemDto>();
        d.CourseClasses ??= new List<CurriculumCourseClassColumnDto>();
        d.DisciplinaTiposOrdered ??= new List<CurriculumDisciplinaTipoRefDto>();
        foreach (var c in d.Classes)
        {
            c.Disciplinas ??= new List<CurriculumDisciplinaModuloDto>();
            foreach (var disc in c.Disciplinas)
            {
                disc.TeacherNames ??= new List<string>();
            }
        }

        return d;
    }

    public async Task<StudentWeeklyScheduleResponseDto?> GetWeeklyScheduleAsync(int? matriculaId = null, CancellationToken cancellationToken = default)
    {
        var url = matriculaId.HasValue ? $"student/schedule/weekly?matricula_id={matriculaId.Value}" : "student/schedule/weekly";
        var (ok, body, status) = await TryGetJsonBodyAsync(url, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            _logger.LogWarning("GET student/schedule/weekly falhou Status={StatusCode} MatriculaId={MatriculaId}", status.HasValue ? (int)status.Value : null, matriculaId);
            return null;
        }

        var wrap = JsonSerializer.Deserialize<ApiResponse<StudentWeeklyScheduleResponseDto>>(body, AppJson.Options);
        return wrap?.Data;
    }

    public async Task<StudentFeesResponseDto?> GetFeesAsync(int? matriculaId = null, CancellationToken cancellationToken = default)
    {
        var url = matriculaId.HasValue ? $"student/fees?matricula_id={matriculaId.Value}" : "student/fees";
        var (ok, body, status) = await TryGetJsonBodyAsync(url, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            _logger.LogWarning("GET student/fees falhou Status={StatusCode} MatriculaId={MatriculaId}", status.HasValue ? (int)status.Value : null, matriculaId);
            return null;
        }

        var wrap = JsonSerializer.Deserialize<ApiResponse<StudentFeesResponseDto>>(body, AppJson.Options);
        return wrap?.Data;
    }

    public Task<DownloadFileResult?> DownloadBoletimAsync(int matriculaId, int periodoId, CancellationToken cancellationToken = default)
    {
        var url = $"student/documents/boletim?matricula_id={matriculaId}&periodo_id={periodoId}";
        return TryDownloadAsync(url, $"boletim_{matriculaId}_{periodoId}.pdf", cancellationToken);
    }

    public Task<DownloadFileResult?> DownloadFichaAcademicaAsync(int matriculaId, CancellationToken cancellationToken = default)
    {
        var url = $"student/documents/ficha-academica?matricula_id={matriculaId}";
        return TryDownloadAsync(url, $"ficha_academica_{matriculaId}.pdf", cancellationToken);
    }
}
