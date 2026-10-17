using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssistenteConversas.Models;
using Microsoft.Extensions.Options;

namespace AssistenteConversas.Services;

// Cliente HTTP único para a API do Poe (compatível com chat/completions).
// Usado pela análise de conversas e pelo assistente de chat.
public sealed class PoeChatClient(HttpClient httpClient, IOptions<PoeOptions> options)
{
    private readonly PoeOptions settings = options.Value;

    public async Task<string> CompleteAsync(IReadOnlyList<ChatMessageItem> messages, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.Model))
            throw new InvalidOperationException("A integração Poe não está configurada.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = settings.Model,
            temperature = settings.Temperature,
            max_tokens = settings.MaxTokens,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("A API de IA recusou a solicitação.");
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body)) throw new InvalidOperationException("A API retornou uma resposta vazia.");

        try
        {
            var envelope = JsonSerializer.Deserialize<PoeResponse>(body) ?? throw new JsonException();
            var content = envelope.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content)) throw new JsonException();
            return content;
        }
        catch (JsonException) { throw new InvalidOperationException("A API retornou dados em formato inválido."); }
    }

    private sealed class PoeResponse
    {
        [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")] public Message? Message { get; set; }
    }

    private sealed class Message
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}
