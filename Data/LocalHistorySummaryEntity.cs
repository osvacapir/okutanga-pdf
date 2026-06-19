using SQLite;

namespace OlondongeApp.Data;

[Table("local_history_summaries")]
public sealed class LocalHistorySummaryEntity
{
    [PrimaryKey]
    public int MatriculaId { get; set; }

    public string? ResultadoFinal { get; set; }

    public int NegativasFinais { get; set; }

    public decimal? MediaFinal { get; set; }

    public int? RankingPosicao { get; set; }

    public int? RankingTotal { get; set; }

    public string? UpdatedAtUtc { get; set; }
}
