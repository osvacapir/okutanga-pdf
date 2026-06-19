using System.Text.Json.Serialization;

namespace OlondongeApp.Models.Dtos;

public sealed class LoginDataDto
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("user")]
    public UserSummaryDto? User { get; set; }
}

public sealed class UserSummaryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("num_processo")]
    public string? NumProcesso { get; set; }
}
