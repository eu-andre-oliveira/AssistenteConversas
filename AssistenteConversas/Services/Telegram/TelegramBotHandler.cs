using System.Globalization;
using AssistenteConversas.Models;
using AssistenteConversas.Services;
using Microsoft.Extensions.Options;

namespace AssistenteConversas.Services.Telegram;

// Trata cada atualização recebida do Telegram: comandos, conversa com o assistente e botões de confirmação.
public sealed class TelegramBotHandler(
    TelegramApi api,
    IChatAssistant assistente,
    ILeadService leadService,
    LeadMatcher matcher,
    LeadChangeApplier applier,
    TelegramSessionStore sessoes,
    IOptions<TelegramOptions> opcoes,
    ILogger<TelegramBotHandler> logger)
{
    private const int UltimosPadrao = 5;
    private const int UltimosMaximo = 20;
    private const string TextoAjuda = """
        Assistente de leads. Você pode:
        - escrever normalmente para cadastrar leads, atualizar campos ou registrar interações;
        - /ultimos [n]: listar os últimos leads (padrão 5, máximo 20);
        - /cancelar: descartar a conversa em andamento;
        - /ajuda: mostrar esta mensagem.
        Nada é salvo sem a sua confirmação pelos botões.
        """;

    private static readonly IReadOnlyList<BotaoTelegram> BotoesConfirmacao =
    [
        new("Confirmar", "confirmar"),
        new("Descartar", "descartar")
    ];

    public async Task ProcessarAsync(TelegramUpdate update, CancellationToken ct)
    {
        if (update.CallbackQuery is { } clique)
        {
            await ProcessarCliqueAsync(clique, ct);
            return;
        }

        if (update.Message is not { Text: { } texto } mensagem) return;
        var chatId = mensagem.Chat.Id;
        if (!EhPermitido(chatId)) return;

        var comando = texto.Trim().Split(' ', 2)[0].ToLowerInvariant();
        switch (comando)
        {
            case "/start":
            case "/ajuda":
                await api.EnviarMensagemAsync(chatId, TextoAjuda, null, ct);
                return;

            case "/cancelar":
                sessoes.Obter(chatId).Reiniciar();
                await api.EnviarMensagemAsync(chatId, "Conversa em andamento descartada.", null, ct);
                return;

            case "/ultimos":
                await EnviarUltimosAsync(chatId, texto, ct);
                return;
        }

        if (texto.StartsWith('/'))
        {
            await api.EnviarMensagemAsync(chatId, "Comando desconhecido. Use /ajuda.", null, ct);
            return;
        }

        await ConversarAsync(chatId, texto, ct);
    }

    private async Task ProcessarCliqueAsync(TelegramCallbackQuery clique, CancellationToken ct)
    {
        // Remove o indicador de carregamento do botão, para o clique não parecer travado.
        await api.ResponderCallbackAsync(clique.Id, ct);

        var chatId = clique.Message?.Chat.Id ?? 0;
        if (chatId == 0 || !EhPermitido(chatId)) return;

        var conversa = sessoes.Obter(chatId);
        switch (clique.Data)
        {
            case "confirmar":
                await ConfirmarAsync(chatId, conversa, ct);
                return;

            case "descartar":
                conversa.Pendente = null;
                conversa.Rascunho = new LeadDraft();
                await api.EnviarMensagemAsync(chatId, "Ação descartada.", null, ct);
                return;
        }
    }

    private async Task ConversarAsync(long chatId, string texto, CancellationToken ct)
    {
        var conversa = sessoes.Obter(chatId);
        var historico = conversa.Historico.TakeLast(10).ToList();
        conversa.Historico.Add(new ChatMessageItem("user", texto));
        conversa.MensagensUsuario.Add(texto);
        conversa.Pendente = null;

        try
        {
            var turno = await assistente.ProcessAsync(texto, historico, BuscarLeads, DateOnly.FromDateTime(DateTime.Now), conversa.Rascunho, ct);
            conversa.Historico.Add(new ChatMessageItem("assistant", turno.Resposta));
            conversa.Rascunho = turno.Rascunho;
            conversa.Pendente = turno.Acao;

            var corpo = turno.Avisos.Count == 0
                ? turno.Resposta
                : $"{turno.Resposta}\n\n{string.Join("\n", turno.Avisos.Select(a => "• " + a))}";

            if (turno.Acao is { } acao)
            {
                await api.EnviarMensagemAsync(chatId, $"{corpo}\n\n{DescreverProposta(acao, turno.Rascunho)}", BotoesConfirmacao, ct);
            }
            else
            {
                await api.EnviarMensagemAsync(chatId, corpo, null, ct);
            }
        }
        catch (InvalidOperationException ex)
        {
            // Mensagens de InvalidOperationException são seguras para o usuário (ex.: integração não configurada).
            await api.EnviarMensagemAsync(chatId, ex.Message, null, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Erro ao processar mensagem do Telegram.");
            await api.EnviarMensagemAsync(chatId, "Não foi possível processar a mensagem. Tente novamente.", null, ct);
        }
    }

    private async Task ConfirmarAsync(long chatId, ConversaTelegram conversa, CancellationToken ct)
    {
        if (conversa.Pendente is not { } acao)
        {
            await api.EnviarMensagemAsync(chatId, "Não há nenhuma ação pendente.", null, ct);
            return;
        }

        var origem = string.Join("\n", conversa.MensagensUsuario);
        try
        {
            await applier.ApplyAsync(acao, origem);
            var mensagem = acao.Tipo == LeadActionType.Criar ? "Lead criado." : "Lead atualizado.";
            conversa.Reiniciar();
            await api.EnviarMensagemAsync(chatId, mensagem, null, ct);
        }
        catch (ArgumentException ex)
        {
            await api.EnviarMensagemAsync(chatId, ex.Message, null, ct);
        }
        catch (InvalidOperationException ex)
        {
            await api.EnviarMensagemAsync(chatId, ex.Message, null, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Erro ao salvar alteração vinda do Telegram.");
            await api.EnviarMensagemAsync(chatId, "Não foi possível salvar a alteração.", null, ct);
        }
    }

    private async Task EnviarUltimosAsync(long chatId, string texto, CancellationToken ct)
    {
        var partes = texto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var quantidade = partes.Length > 1 && int.TryParse(partes[1], CultureInfo.InvariantCulture, out var n)
            ? Math.Clamp(n, 1, UltimosMaximo)
            : UltimosPadrao;

        var leads = (await leadService.SearchAsync(null, null, null, null)).Take(quantidade).ToList();
        if (leads.Count == 0)
        {
            await api.EnviarMensagemAsync(chatId, "Nenhum lead cadastrado.", null, ct);
            return;
        }

        var linhas = leads.Select((lead, i) =>
            $"{i + 1}. {lead.Nome} | {lead.Empresa ?? "sem empresa"} | {lead.Status} | follow-up: {lead.ProximoFollowUp?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—"}");
        await api.EnviarMensagemAsync(chatId, $"Últimos {leads.Count} leads:\n" + string.Join("\n", linhas), null, ct);
    }

    private static string DescreverProposta(LeadAction acao, LeadDraft rascunho)
    {
        var titulo = acao.Tipo == LeadActionType.Criar
            ? "Ação proposta: criar lead"
            : $"Ação proposta: atualizar lead {rascunho.LeadNome}";

        var linhas = acao.Campos.GetChanges().Select(c => $"{c.Campo}: {c.Valor}").ToList();
        if (acao.Interacao is not null) linhas.Add($"Registrar no histórico: {acao.Interacao}");
        return titulo + "\n" + string.Join("\n", linhas);
    }

    private async Task<IReadOnlyList<LeadCandidate>> BuscarLeads(LeadSearchCriteria criterio) =>
        matcher.Buscar(await leadService.SearchAsync(null, null, null, null), criterio);

    private bool EhPermitido(long chatId) => opcoes.Value.AllowedChatIds.Contains(chatId);
}
