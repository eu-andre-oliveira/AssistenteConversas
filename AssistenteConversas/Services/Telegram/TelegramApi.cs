using System.Net.Http.Json;
using System.Text.Json;
using AssistenteConversas.Models;

namespace AssistenteConversas.Services.Telegram;

// Cliente mínimo da API do Telegram Bot. O endereço base já inclui o token (definido em Program.cs).
public sealed class TelegramApi(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<IReadOnlyList<TelegramUpdate>> ObterAtualizacoesAsync(long offset, int timeoutSegundos, CancellationToken ct)
    {
        using var resposta = await http.PostAsJsonAsync(
            "getUpdates",
            new { offset, timeout = timeoutSegundos, allowed_updates = new[] { "message", "callback_query" } },
            Json,
            ct);
        return await LerAsync<IReadOnlyList<TelegramUpdate>>(resposta, ct) ?? [];
    }

    public async Task EnviarMensagemAsync(long chatId, string texto, IReadOnlyList<BotaoTelegram>? botoes, CancellationToken ct)
    {
        // Sem botões, a mensagem é enviada sem reply_markup.
        using var resposta = botoes is null
            ? await http.PostAsJsonAsync("sendMessage", new { chat_id = chatId, text = texto }, Json, ct)
            : await http.PostAsJsonAsync("sendMessage", new
            {
                chat_id = chatId,
                text = texto,
                reply_markup = new { inline_keyboard = new[] { botoes.Select(b => new { text = b.Texto, callback_data = b.Dados }).ToArray() } }
            }, Json, ct);
        await LerAsync<JsonElement?>(resposta, ct);
    }

    // Remove o indicador de carregamento do botão clicado.
    public async Task ResponderCallbackAsync(string callbackId, CancellationToken ct)
    {
        using var resposta = await http.PostAsJsonAsync("answerCallbackQuery", new { callback_query_id = callbackId }, Json, ct);
        await LerAsync<JsonElement?>(resposta, ct);
    }

    private static async Task<T?> LerAsync<T>(HttpResponseMessage resposta, CancellationToken ct)
    {
        var corpo = await resposta.Content.ReadFromJsonAsync<TelegramResposta<T>>(Json, ct);
        if (corpo is null || !corpo.Ok)
            throw new InvalidOperationException($"O Telegram recusou a requisição: {corpo?.Description ?? resposta.StatusCode.ToString()}");
        return corpo.Result;
    }
}
