using System.Text.Json.Serialization;

namespace OlondongeApp.Models.Dtos;

public sealed class PeriodRowDto
{
    [JsonPropertyName("avaliacao_id")]
    public int? AvaliacaoId { get; set; }

    [JsonPropertyName("matricula_id")]
    public int MatriculaId { get; set; }

    [JsonPropertyName("periodo_id")]
    public int PeriodoId { get; set; }

    [JsonPropertyName("periodo_name")]
    public string? PeriodoName { get; set; }
}
