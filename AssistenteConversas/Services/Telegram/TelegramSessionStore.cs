using System.Collections.Concurrent;
using AssistenteConversas.Models;

namespace AssistenteConversas.Services.Telegram;

// Estado de uma conversa do Telegram, guardado em memória por chat.
public sealed class ConversaTelegram
{
    public List<ChatMessageItem> Historico { get; } = [];
    public List<string> MensagensUsuario { get; } = [];
    public LeadDraft Rascunho { get; set; } = new();
    public LeadAction? Pendente { get; set; }

    public void Reiniciar()
    {
        Historico.Clear();
        MensagensUsuario.Clear();
        Rascunho = new LeadDraft();
        Pendente = null;
    }
}

public sealed class TelegramSessionStore
{
    private readonly ConcurrentDictionary<long, ConversaTelegram> conversas = new();

    public ConversaTelegram Obter(long chatId) => conversas.GetOrAdd(chatId, _ => new ConversaTelegram());
}
