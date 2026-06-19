using System.Text.Json.Serialization;

namespace OlondongeApp.Models.Dtos;

public sealed class EnrolmentDto
{
    [JsonPropertyName("matricula_id")]
    public int MatriculaId { get; set; }

    [JsonPropertyName("turma_id")]
    public int TurmaId { get; set; }

    [JsonPropertyName("curso_id")]
    public int CursoId { get; set; }

    [JsonPropertyName("ano_lectivo_id")]
    public int AnoLectivoId { get; set; }

    [JsonPropertyName("is_current_ano_lectivo")]
    [JsonConverter(typeof(FlexibleBooleanJsonConverter))]
    public bool IsCurrentAnoLectivo { get; set; }

    [JsonPropertyName("classe_name")]
    public string? ClasseName { get; set; }

    [JsonPropertyName("curso_name")]
    public string? CursoName { get; set; }

    [JsonPropertyName("curso_abreviatura")]
    public string? CursoAbreviatura { get; set; }

    [JsonPropertyName("ano_lectivo_name")]
    public string? AnoLectivoName { get; set; }

    /// <summary>Nome do turno da turma (ex.: Manhã, Tarde), quando existir na escola.</summary>
    [JsonPropertyName("turno_name")]
    public string? TurnoName { get; set; }
}
