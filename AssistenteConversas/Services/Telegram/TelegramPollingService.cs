using AssistenteConversas.Models;
using Microsoft.Extensions.Options;

namespace AssistenteConversas.Services.Telegram;

// Consulta o Telegram em long polling. Não precisa de URL pública: a aplicação pergunta ao Telegram por novas mensagens.
public sealed class TelegramPollingService(
    TelegramApi api,
    IServiceScopeFactory escopos,
    IOptions<TelegramOptions> opcoes,
    ILogger<TelegramPollingService> logger) : BackgroundService
{
    private static readonly TimeSpan EsperaEntreFalhas = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var settings = opcoes.Value;
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.BotToken))
        {
            logger.LogInformation("Integração com o Telegram desativada.");
            return;
        }

        logger.LogInformation("Integração com o Telegram ativa (long polling).");
        long offset = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var atualizacoes = await api.ObterAtualizacoesAsync(offset, settings.PollingTimeoutSeconds, ct);
                foreach (var atualizacao in atualizacoes)
                {
                    // Confirma a atualização ao Telegram antes de processá-la, para não receber a mesma duas vezes.
                    offset = atualizacao.UpdateId + 1;
                    await ProcessarAsync(atualizacao, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Só o tipo do erro é registrado, para não vazar a URL com o token.
                logger.LogWarning("Falha ao consultar o Telegram ({Tipo}). Nova tentativa em alguns segundos.", ex.GetType().Name);
                await Task.Delay(EsperaEntreFalhas, ct);
            }
        }
    }

    private async Task ProcessarAsync(TelegramUpdate atualizacao, CancellationToken ct)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var handler = escopo.ServiceProvider.GetRequiredService<TelegramBotHandler>();
        try
        {
            await handler.ProcessarAsync(atualizacao, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar atualização do Telegram.");
        }
    }
}
