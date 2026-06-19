using OlondongeApp.Models.Dtos;

namespace OlondongeApp.Models;

/// <summary>Texto de matrícula para UI (dropdown, subtítulos). Dados vêm da API/SQLite (<c>classe_name</c>, <c>curso_name</c>, <c>curso_abreviatura</c>).</summary>
public static class EnrolmentDisplayFormatter
{
    private const int MaxSegmentLength = 48;

    /// <summary>Ex.: «12ª - BIO/QUI» — classe + abreviatura do curso; se não houver abreviatura, usa o nome do curso.</summary>
    public static string FormatMatriculaSelectLabel(EnrolmentDto e)
    {
        var classe = TrimDisplay(e.ClasseName);
        var curso = FormatCursoDisplay(e);

        if (classe.Length == 0 && curso.Length == 0)
        {
            return $"Matrícula {e.MatriculaId}";
        }

        if (classe.Length == 0)
        {
            return curso;
        }

        if (curso.Length == 0)
        {
            return classe;
        }

        return $"{classe} - {curso}";
    }

    /// <summary>Nome da classe tal como na BD (<c>classes.name</c> via <c>classe_name</c>).</summary>
    public static string FormatClasseShort(string? classeName)
        => TrimDisplay(classeName);

    /// <summary>Segmento «curso» no selector: abreviatura se existir; senão nome completo.</summary>
    public static string FormatCursoDisplay(EnrolmentDto e)
    {
        var abv = TrimDisplay(e.CursoAbreviatura);
        if (abv.Length > 0)
        {
            return abv;
        }

        return TrimDisplay(e.CursoName);
    }

    /// <summary>Texto longo para <c>title</c> na opção (nome e abreviatura quando ambos existem).</summary>
    public static string? FormatMatriculaOptionTitle(EnrolmentDto e)
    {
        var nome = TrimDisplay(e.CursoName);
        var abv = TrimDisplay(e.CursoAbreviatura);
        if (nome.Length == 0 && abv.Length == 0)
        {
            return null;
        }

        if (nome.Length > 0 && abv.Length > 0 && !string.Equals(nome, abv, StringComparison.OrdinalIgnoreCase))
        {
            return $"{nome} · {abv}";
        }

        if (nome.Length > 0)
        {
            return nome;
        }

        return abv.Length > 0 ? abv : null;
    }

    private static string TrimDisplay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var t = value.Trim();
        return t.Length > MaxSegmentLength ? t[..MaxSegmentLength] + "…" : t;
    }
}
