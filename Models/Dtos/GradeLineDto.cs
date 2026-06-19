using System.Text.Json.Serialization;
using System.Text.Json;

namespace OlondongeApp.Models.Dtos;

public sealed class GradeLineDto
{
    [JsonPropertyName("avaliacao_id")]
    public int AvaliacaoId { get; set; }

    [JsonPropertyName("matricula_id")]
    public int MatriculaId { get; set; }

    [JsonPropertyName("disciplina_name")]
    public string? DisciplinaName { get; set; }

    [JsonPropertyName("disciplina_abreviatura")]
    public string? DisciplinaAbreviatura { get; set; }

    [JsonPropertyName("periodo_id")]
    public int PeriodoId { get; set; }

    [JsonPropertyName("periodo_name")]
    public string? PeriodoName { get; set; }

    [JsonPropertyName("mac")]
    public decimal? Mac { get; set; }

    [JsonPropertyName("cpp")]
    public decimal? Cpp { get; set; }

    [JsonPropertyName("cpt")]
    public decimal? Cpt { get; set; }

    [JsonPropertyName("mt")]
    public decimal? Mt { get; set; }

    [JsonPropertyName("cpp_enabled")]
    [JsonConverter(typeof(FlexibleBoolConverter))]
    public bool CppEnabled { get; set; }

    [JsonPropertyName("exames")]
    public List<GradeExamDto>? Exames { get; set; }
}

public sealed class GradeExamDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("valor")]
    public decimal? Valor { get; set; }

    [JsonPropertyName("tipo_avaliacao_name")]
    public string? TipoAvaliacaoName { get; set; }
}

public sealed class FlexibleBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.True) return true;
        if (reader.TokenType == JsonTokenType.False) return false;

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var n)) return n != 0;
            if (reader.TryGetDouble(out var d)) return Math.Abs(d) > double.Epsilon;
            return false;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString()?.Trim();
            if (string.IsNullOrEmpty(s)) return false;

            if (bool.TryParse(s, out var b)) return b;
            if (int.TryParse(s, out var n)) return n != 0;
            if (double.TryParse(s, out var d)) return Math.Abs(d) > double.Epsilon;
        }

        if (reader.TokenType == JsonTokenType.Null) return false;

        throw new JsonException("Valor inválido para booleano flexível.");
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
