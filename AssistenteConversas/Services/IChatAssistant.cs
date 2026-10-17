using AssistenteConversas.Models;

namespace AssistenteConversas.Services;

public interface IChatAssistant
{
    Task<AssistantTurn> ProcessAsync(
        string mensagem,
        IReadOnlyList<ChatMessageItem> historico,
        IReadOnlyList<Lead> leads,
        DateOnly hoje,
        CancellationToken cancellationToken = default);
}
