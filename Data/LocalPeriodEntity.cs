using SQLite;

namespace OlondongeApp.Data;

[Table("local_periods")]
public sealed class LocalPeriodEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int MatriculaId { get; set; }

    public int PeriodoId { get; set; }

    public string? PeriodoName { get; set; }
}
