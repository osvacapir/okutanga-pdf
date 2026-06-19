using SQLite;

namespace OlondongeApp.Data;

[Table("grade_lines")]
public sealed class LocalGradeLineEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int MatriculaId { get; set; }

    public int PeriodoId { get; set; }

    public int AvaliacaoId { get; set; }

    public int SortOrder { get; set; }

    public string Json { get; set; } = string.Empty;
}
