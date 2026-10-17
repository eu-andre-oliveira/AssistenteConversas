namespace AssistenteConversas.Models;

// Resposta do assistente já validada pelo servidor.
// Acao só vem preenchida quando a proposta está completa; Rascunho é o estado a guardar para a próxima mensagem.
public sealed record AssistantTurn(string Resposta, LeadAction? Acao, IReadOnlyList<string> Avisos, LeadDraft Rascunho);

// Ação proposta pelo assistente. Nunca é aplicada sem confirmação do usuário.
public sealed record LeadAction(LeadActionType Tipo, Guid? LeadId, LeadPatch Campos, string? Interacao);

public enum LeadActionType
{
    Criar,
    Atualizar
}

// Mensagem do histórico enviada ao modelo.
public sealed record ChatMessageItem(string Role, string Content);
