using System.Text.Json.Serialization;

namespace OlondongeApp.Models.Dtos;

public sealed class StudentWeeklyScheduleResponseDto
{
    [JsonPropertyName("matricula_id")]
    public int MatriculaId { get; set; }

    [JsonPropertyName("turma_id")]
    public int TurmaId { get; set; }

    [JsonPropertyName("slots")]
    public List<WeeklyScheduleSlotDto> Slots { get; set; } = new();
}

public sealed class WeeklyScheduleSlotDto
{
    [JsonPropertyName("weekday")]
    public int Weekday { get; set; }

    [JsonPropertyName("weekday_name")]
    public string? WeekdayName { get; set; }

    [JsonPropertyName("horario_slot_id")]
    public int HorarioSlotId { get; set; }

    [JsonPropertyName("slot_name")]
    public string? SlotName { get; set; }

    [JsonPropertyName("slot_order")]
    public int SlotOrder { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    [JsonPropertyName("disciplina_name")]
    public string? DisciplinaName { get; set; }

    [JsonPropertyName("disciplina_abreviatura")]
    public string? DisciplinaAbreviatura { get; set; }

    [JsonPropertyName("teacher_name")]
    public string? TeacherName { get; set; }
}
