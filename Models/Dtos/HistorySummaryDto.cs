using System.Text.Json.Serialization;

namespace OlondongeApp.Models.Dtos;

public sealed class HistorySummaryDto
{
    [JsonPropertyName("matricula_id")]
    public int MatriculaId { get; set; }

    [JsonPropertyName("resultado_final")]
    public string? ResultadoFinal { get; set; }

    [JsonPropertyName("negativas_finais")]
    public int NegativasFinais { get; set; }

    [JsonPropertyName("media_final")]
    public decimal? MediaFinal { get; set; }

    [JsonPropertyName("ranking_posicao")]
    public int? RankingPosicao { get; set; }

    [JsonPropertyName("ranking_total")]
    public int? RankingTotal { get; set; }
}
