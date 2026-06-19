namespace OlondongeApp.Models;

/// <summary>Cores de notas (0–20): abaixo de 10 vermelho, 15 ou mais verde, resto cor por defeito.</summary>
public static class GradeValueDisplay
{
    public static string CssClassFor(decimal? value)
    {
        if (!value.HasValue)
        {
            return string.Empty;
        }

        var v = value.Value;
        if (v < 10m)
        {
            return "ol-grade-value--low";
        }

        if (v >= 15m)
        {
            return "ol-grade-value--high";
        }

        return string.Empty;
    }
}
