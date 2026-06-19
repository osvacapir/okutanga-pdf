using System.Text.Json.Serialization;

namespace OlondongeApp.Models.Dtos;

public sealed class GradesSyncStatusDto
{
    [JsonPropertyName("grades_version")]
    public string? GradesVersion { get; set; }

    [JsonPropertyName("grades_version_iso")]
    public string? GradesVersionIso { get; set; }
}
