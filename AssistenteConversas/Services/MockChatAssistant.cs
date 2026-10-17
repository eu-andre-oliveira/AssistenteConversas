using AssistenteConversas.Models;

namespace AssistenteConversas.Services;

public sealed class MockChatAssistant : IChatAssistant
{
    public Task<AssistantTurn> ProcessAsync(
        string mensagem,
        IReadOnlyList<ChatMessageItem> historico,
        IReadOnlyList<Lead> leads,
        DateOnly hoje,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AssistantTurn(
            "Modo mock: nenhuma ação foi proposta. Configure o provedor Poe para conversar com o assistente.",
            Acao: null,
            Avisos: []));
    }
}
