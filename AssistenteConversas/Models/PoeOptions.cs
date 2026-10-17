namespace AssistenteConversas.Models;

public sealed class PoeOptions
{
    public string Provider { get; set; } = "Mock";
    public string BaseUrl { get; set; } = "https://api.poe.com/v1";
    public string Model { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 2000;
    public int TimeoutSeconds { get; set; } = 90;
}
