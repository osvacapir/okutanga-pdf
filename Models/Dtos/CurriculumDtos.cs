using System.Text.Json.Serialization;

namespace OlondongeApp.Models.Dtos;

public sealed class StudentCurriculumResponseDto
{
    [JsonPropertyName("curso_id")]
    public int CursoId { get; set; }

    [JsonPropertyName("curso_nome")]
    public string? CursoName { get; set; }

    /// <summary>Abreviatura do curso (<c>cursos.abreviatura</c>), quando a API a envia no payload do currículo.</summary>
    [JsonPropertyName("curso_abreviatura")]
    public string? CursoAbreviatura { get; set; }

    [JsonPropertyName("matricula_id")]
    public int MatriculaId { get; set; }

    [JsonPropertyName("turma_id")]
    public int TurmaId { get; set; }

    /// <summary>Contagem de linhas devolvidas pela query agregadora de módulos (curso/escola na API).</summary>
    [JsonPropertyName("modulos_total")]
    public int ModulosTotal { get; set; }

    [JsonPropertyName("disciplinas_total")]
    public int DisciplinasTotal { get; set; }

    [JsonPropertyName("classes_distintas_total")]
    public int ClassesDistintasTotal { get; set; }

    /// <summary>Colunas do curso (classes), como na ficha académica web.</summary>
    [JsonPropertyName("course_classes")]
    public List<CurriculumCourseClassColumnDto> CourseClasses { get; set; } = new();

    /// <summary>Ordem das secções por tipo de disciplina (nome + id, como na web).</summary>
    [JsonPropertyName("disciplina_tipos_ordered")]
    public List<CurriculumDisciplinaTipoRefDto> DisciplinaTiposOrdered { get; set; } = new();

    /// <summary>Lista plana (contrato antigo); preferir <see cref="Classes"/> quando vindo da API nova.</summary>
    [JsonPropertyName("items")]
    public List<CurriculumItemDto> Items { get; set; } = new();

    /// <summary>Por classe, com disciplinas (módulos) aninhadas — formato actual do estudante.</summary>
    [JsonPropertyName("classes")]
    public List<CurriculumClasseGroupDto> Classes { get; set; } = new();
}

public sealed class CurriculumClasseGroupDto
{
    [JsonPropertyName("classe_id")]
    public int ClasseId { get; set; }

    [JsonPropertyName("classe_nome")]
    public string? ClasseName { get; set; }

    [JsonPropertyName("disciplinas")]
    public List<CurriculumDisciplinaModuloDto> Disciplinas { get; set; } = new();
}

/// <summary>Uma disciplina/módulo dentro de uma classe na resposta do currículo.</summary>
public sealed class CurriculumDisciplinaModuloDto
{
    [JsonPropertyName("modulo_id")]
    public int ModuloId { get; set; }

    [JsonPropertyName("curso_disciplina_id")]
    public int CursoDisciplinaId { get; set; }

    [JsonPropertyName("disciplina_nome")]
    public string? DisciplinaNome { get; set; }

    [JsonPropertyName("disciplina_abreviatura")]
    public string? DisciplinaAbreviatura { get; set; }

    [JsonPropertyName("teacher_names")]
    public List<string> TeacherNames { get; set; } = new();
}

public sealed class CurriculumCourseClassColumnDto
{
    [JsonPropertyName("classe_id")]
    public int ClasseId { get; set; }

    [JsonPropertyName("classe_name")]
    public string? ClasseName { get; set; }
}

public sealed class CurriculumDisciplinaTipoRefDto
{
    [JsonPropertyName("tipo_id")]
    public int TipoId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class CurriculumCursoClasseDto
{
    [JsonPropertyName("classe_id")]
    public int ClasseId { get; set; }

    [JsonPropertyName("classe_name")]
    public string? ClasseName { get; set; }
}

public sealed class CurriculumItemDto
{
    [JsonPropertyName("modulo_id")]
    public int ModuloId { get; set; }

    [JsonPropertyName("classe_id")]
    public int ClasseId { get; set; }

    [JsonPropertyName("disciplina_id")]
    public int DisciplinaId { get; set; }

    [JsonPropertyName("curso_disciplina_id")]
    public int CursoDisciplinaId { get; set; }

    [JsonPropertyName("disciplina_tipo_id")]
    public int DisciplinaTipoId { get; set; }

    [JsonPropertyName("disciplina_tipo_name")]
    public string? DisciplinaTipoName { get; set; }

    [JsonPropertyName("disciplina_nome")]
    public string? DisciplinaName { get; set; }

    [JsonPropertyName("disciplina_abreviatura")]
    public string? DisciplinaAbreviatura { get; set; }

    [JsonPropertyName("classe_nome")]
    public string? ClasseName { get; set; }

    [JsonPropertyName("classes")]
    public List<CurriculumCursoClasseDto> Classes { get; set; } = new();

    [JsonPropertyName("curso_name")]
    public string? CursoName { get; set; }

    [JsonPropertyName("curso_abreviatura")]
    public string? CursoAbreviatura { get; set; }

    [JsonPropertyName("ano_lectivo_name")]
    public string? AnoLectivoName { get; set; }

    /// <summary><c>1</c> quando existe módulo na célula; <c>0</c> quando a disciplina do curso ainda não tem módulo nessa classe.</summary>
    [JsonPropertyName("modules_count")]
    public int ModulesCount { get; set; }

    [JsonPropertyName("teacher_names")]
    public List<string> TeacherNames { get; set; } = new();

    [JsonPropertyName("disciplina_tipo_names")]
    public List<string> DisciplinaTipoNames { get; set; } = new();
}
