using SQLite;

namespace OlondongeApp.Data;

[Table("local_enrolments")]
public sealed class LocalEnrolmentEntity
{
    [PrimaryKey]
    public int MatriculaId { get; set; }

    public int TurmaId { get; set; }

    public int CursoId { get; set; }

    public int AnoLectivoId { get; set; }

    public int IsCurrentAnoLectivo { get; set; }

    public string? ClasseName { get; set; }

    public string? CursoName { get; set; }

    public string? CursoAbreviatura { get; set; }

    public string? AnoLectivoName { get; set; }

    public string? TurnoName { get; set; }
}
