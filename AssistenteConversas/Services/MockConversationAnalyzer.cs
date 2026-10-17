using AssistenteConversas.Models;

namespace AssistenteConversas.Services;

public sealed class MockConversationAnalyzer : IConversationAnalyzer
{
    public Task<ConversationAnalysisResult> AnalyzeAsync(
        string conversation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversation))
        {
            throw new ArgumentException("A conversa não pode estar vazia.", nameof(conversation));
        }

        return Task.FromResult(new ConversationAnalysisResult
        {
            Temperatura = LeadValues.TemperaturaIndefinido,
            Confianca = null,
            Observacoes = "Modo Mock: nenhum dado foi inventado. Substitua o provedor por Poe para analisar a conversa."
        });
    }
}
