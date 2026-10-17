using System.Text.Json;
using AssistenteConversas.Models;
using AssistenteConversas.Prompts;

namespace AssistenteConversas.Services;

public sealed class PoeConversationAnalyzer(PoeChatClient client) : IConversationAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ConversationAnalysisResult> AnalyzeAsync(string conversation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversation)) throw new ArgumentException("A conversa não pode estar vazia.", nameof(conversation));

        var content = await client.CompleteAsync(
        [
            new ChatMessageItem("system", ConversationAnalysisPrompt.System),
            new ChatMessageItem("user", conversation)
        ], cancellationToken);

        try
        {
            var result = JsonSerializer.Deserialize<ConversationAnalysisResult>(LlmJson.ExtractObject(content), JsonOptions) ?? throw new JsonException();
            result.Temperatura = LeadValues.Temperaturas.Contains(result.Temperatura ?? string.Empty) ? result.Temperatura : LeadValues.TemperaturaIndefinido;
            if (result.Confianca is < 0 or > 1) result.Confianca = null;
            return result;
        }
        catch (JsonException) { throw new InvalidOperationException("A API retornou dados em formato inválido."); }
    }
}
