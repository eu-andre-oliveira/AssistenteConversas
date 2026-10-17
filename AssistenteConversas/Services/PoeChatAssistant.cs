using System.Globalization;
using System.Text.Json;
using AssistenteConversas.Models;
using AssistenteConversas.Prompts;

namespace AssistenteConversas.Services;

// Converte a mensagem do usuário em uma resposta e, quando for o caso, em uma ação proposta.
// O modelo sugere; o servidor valida tudo antes de devolver, e nada é salvo aqui.
public sealed class PoeChatAssistant(PoeChatClient client) : IChatAssistant
{
    private const string FormatoData = "yyyy-MM-dd";
    private const int MaxNome = 200;
    private const int MaxInteracao = 4000;

    public async Task<AssistantTurn> ProcessAsync(
        string mensagem,
        IReadOnlyList<ChatMessageItem> historico,
        IReadOnlyList<Lead> leads,
        DateOnly hoje,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mensagem)) throw new ArgumentException("A mensagem não pode estar vazia.", nameof(mensagem));

        var messages = new List<ChatMessageItem> { new("system", ChatAssistantPrompt.Build(hoje, leads)) };
        messages.AddRange(historico);
        messages.Add(new ChatMessageItem("user", mensagem));

        var content = await client.CompleteAsync(messages, cancellationToken);

        try
        {
            using var document = JsonDocument.Parse(LlmJson.ExtractObject(content));
            return Parse(document.RootElement, leads);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("O assistente respondeu em formato inválido. Tente reformular a mensagem.");
        }
    }

    private static AssistantTurn Parse(JsonElement root, IReadOnlyList<Lead> leads)
    {
        var avisos = new List<string>();
        var resposta = Texto(root, "resposta") ?? "Não consegui montar uma resposta.";
        LeadAction? acao = null;

        if (root.TryGetProperty("acao", out var acaoJson) && acaoJson.ValueKind == JsonValueKind.Object)
        {
            acao = ParseAcao(acaoJson, leads, avisos);
        }

        return new AssistantTurn(resposta, acao, avisos);
    }

    private static LeadAction? ParseAcao(JsonElement acaoJson, IReadOnlyList<Lead> leads, List<string> avisos)
    {
        var tipo = Texto(acaoJson, "tipo")?.ToLowerInvariant();
        var campos = acaoJson.TryGetProperty("campos", out var camposJson) && camposJson.ValueKind == JsonValueKind.Object
            ? camposJson
            : default;
        var interacao = Limitar(Texto(acaoJson, "interacao"), MaxInteracao, avisos, "Interação");

        switch (tipo)
        {
            case "criar":
            {
                var patch = ParsePatch(campos, avisos);
                if (string.IsNullOrWhiteSpace(patch.Nome))
                {
                    avisos.Add("Não é possível criar o lead sem um nome. Informe o nome e tente novamente.");
                    return null;
                }
                return new LeadAction(LeadActionType.Criar, null, patch, interacao);
            }

            case "atualizar":
            {
                var leadId = Texto(acaoJson, "leadId");
                if (!Guid.TryParse(leadId, out var id) || !leads.Any(lead => lead.Id == id))
                {
                    avisos.Add("Não consegui identificar o lead com segurança. Nenhuma alteração foi proposta.");
                    return null;
                }

                var patch = ParsePatch(campos, avisos);
                if (patch.IsEmpty && interacao is null) return null;
                return new LeadAction(LeadActionType.Atualizar, id, patch, interacao);
            }

            default:
                return null;
        }
    }

    private static LeadPatch ParsePatch(JsonElement campos, List<string> avisos)
    {
        if (campos.ValueKind != JsonValueKind.Object) return new LeadPatch();

        var patch = new LeadPatch
        {
            Nome = Limitar(Texto(campos, "nome"), MaxNome, avisos, "Nome"),
            Empresa = Texto(campos, "empresa"),
            Cargo = Texto(campos, "cargo"),
            Email = Texto(campos, "email"),
            Telefone = Texto(campos, "telefone"),
            PerfilLinkedIn = Texto(campos, "perfilLinkedIn"),
            Necessidade = Texto(campos, "necessidade"),
            ProdutoInteresse = Texto(campos, "produtoInteresse"),
            NivelInteresse = Texto(campos, "nivelInteresse"),
            PrazoEstimado = Texto(campos, "prazoEstimado"),
            ProximaAcao = Texto(campos, "proximaAcao"),
            Resumo = Texto(campos, "resumo"),
            Observacoes = Texto(campos, "observacoes"),
            Temperatura = Lista(campos, "temperatura", LeadValues.Temperaturas, "Temperatura", avisos),
            Status = Lista(campos, "status", LeadValues.Statuses, "Status", avisos),
            ProximoFollowUp = Data(campos, "proximoFollowUp", avisos),
        };
        return patch;
    }

    // Valor precisa estar na lista fechada (sem diferenciar maiúsculas); caso contrário é descartado com aviso.
    private static string? Lista(JsonElement obj, string propriedade, IEnumerable<string> permitidos, string rotulo, List<string> avisos)
    {
        var valor = Texto(obj, propriedade);
        if (valor is null) return null;

        var canonico = permitidos.FirstOrDefault(p => string.Equals(p, valor, StringComparison.OrdinalIgnoreCase));
        if (canonico is null) avisos.Add($"{rotulo} \"{valor}\" não é válido e foi ignorado.");
        return canonico;
    }

    private static string? Data(JsonElement obj, string propriedade, List<string> avisos)
    {
        var valor = Texto(obj, propriedade);
        if (valor is null) return null;

        if (DateOnly.TryParseExact(valor, FormatoData, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
            return data.ToString(FormatoData, CultureInfo.InvariantCulture);

        avisos.Add($"Data \"{valor}\" inválida. Use o formato aaaa-mm-dd. O follow-up foi ignorado.");
        return null;
    }

    private static string? Limitar(string? valor, int max, List<string> avisos, string rotulo)
    {
        if (valor is null || valor.Length <= max) return valor;
        avisos.Add($"{rotulo} foi cortado para {max} caracteres.");
        return valor[..max];
    }

    // Lê uma propriedade texto; valores vazios, nulos ou de outro tipo viram null.
    private static string? Texto(JsonElement obj, string propriedade)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        if (!obj.TryGetProperty(propriedade, out var valor) || valor.ValueKind != JsonValueKind.String) return null;
        var texto = valor.GetString()?.Trim();
        return string.IsNullOrEmpty(texto) ? null : texto;
    }
}
