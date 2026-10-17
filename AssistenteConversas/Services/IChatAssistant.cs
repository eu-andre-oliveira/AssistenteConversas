using AssistenteConversas.Models;

namespace AssistenteConversas.Services;

public interface IChatAssistant
{
    // "buscar" localiza leads existentes; "rascunho" é o estado de uma intenção em andamento entre mensagens.
    Task<AssistantTurn> ProcessAsync(
        string mensagem,
        IReadOnlyList<ChatMessageItem> historico,
        Func<LeadSearchCriteria, Task<IReadOnlyList<LeadCandidate>>> buscar,
        DateOnly hoje,
        LeadDraft rascunho,
        CancellationToken cancellationToken = default);
}
