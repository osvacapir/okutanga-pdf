using System.Text.Json.Serialization;

namespace OlondongeApp.Models.Dtos;

public sealed class StudentFeesResponseDto
{
    [JsonPropertyName("matricula_id")]
    public int MatriculaId { get; set; }

    [JsonPropertyName("summary")]
    public StudentFeesSummaryDto? Summary { get; set; }

    [JsonPropertyName("missing_months")]
    public List<StudentMissingMonthDto> MissingMonths { get; set; } = new();

    [JsonPropertyName("payments")]
    public List<StudentFeePaymentDto> Payments { get; set; } = new();
}

public sealed class StudentFeesSummaryDto
{
    [JsonPropertyName("missing_count")]
    public int MissingCount { get; set; }

    [JsonPropertyName("next_missing")]
    public StudentMissingMonthDto? NextMissing { get; set; }

    [JsonPropertyName("paid_total")]
    public decimal PaidTotal { get; set; }

    [JsonPropertyName("recent_payments_count")]
    public int RecentPaymentsCount { get; set; }
}

public sealed class StudentMissingMonthDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("month")]
    public int? Month { get; set; }

    [JsonPropertyName("year")]
    public int? Year { get; set; }
}

public sealed class StudentFeePaymentDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("paid_at")]
    public string? PaidAt { get; set; }
}
