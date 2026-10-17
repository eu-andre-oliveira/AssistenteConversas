using System.Globalization;
using System.Text.Json;
using AssistenteConversas.Models;
using AssistenteConversas.Prompts;

namespace AssistenteConversas.Services;

// Conduz a conversa em um laço: o modelo pode pedir buscas de leads (protocolo "buscar") antes de responder.
// O rascunho da intenção é mesclado a cada resposta; a proposta só é montada quando o modelo diz "pronto".
// O servidor valida tudo e nada é salvo aqui.
public sealed class PoeChatAssistant(PoeChatClient client, ILeadService leadService, ILogger<PoeChatAssistant> logger) : IChatAssistant
{
    private const string FormatoData = "yyyy-MM-dd";
    private const int MaxNome = 200;
    private const int MaxInteracao = 4000;
    private const int MaxBuscas = 3;
    private const int MaxReparos = 1;
    private const double LimiteDuplicado = 0.9;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Dados lidos de uma resposta do modelo, ainda sem mesclar com o rascunho.
    private sealed record Parcial(LeadActionType? Tipo, Guid? LeadId, string? LeadNome, LeadPatch Campos, string? Interacao);

    public async Task<AssistantTurn> ProcessAsync(
        string mensagem,
        IReadOnlyList<ChatMessageItem> historico,
        Func<LeadSearchCriteria, Task<IReadOnlyList<LeadCandidate>>> buscar,
        DateOnly hoje,
        LeadDraft rascunho,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mensagem)) throw new ArgumentException("A mensagem não pode estar vazia.", nameof(mensagem));

        var mensagens = new List<ChatMessageItem> { new("system", ChatAssistantPrompt.Build(hoje, rascunho)) };
        mensagens.AddRange(historico);
        mensagens.Add(new ChatMessageItem("user", mensagem));

        var encontrados = new Dictionary<Guid, string>();
        var avisos = new List<string>();
        var buscas = 0;
        var reparos = 0;

        for (var passo = 0; passo < MaxBuscas + MaxReparos + 1; passo++)
        {
            var content = await client.CompleteAsync(mensagens, cancellationToken);
            using var document = AbrirJson(content);

            if (document is null)
            {
                // Resposta sem JSON: pede uma única correção. Se falhar de novo, vira só texto, sem ação e mantendo o rascunho.
                if (reparos == MaxReparos) return new AssistantTurn(content.Trim(), null, avisos, rascunho);
                reparos++;
                mensagens.Add(new ChatMessageItem("assistant", content));
                mensagens.Add(new ChatMessageItem("user", "Sua resposta não estava em JSON válido. Responda novamente somente com o JSON no formato pedido."));
                continue;
            }
            var root = document.RootElement;

            if (root.TryGetProperty("buscar", out var buscaJson) && buscaJson.ValueKind == JsonValueKind.Object)
            {
                // Limite de buscas atingido: sai do laço e devolve a pergunta padrão.
                if (buscas == MaxBuscas) break;
                buscas++;

                var criterio = new LeadSearchCriteria(Texto(buscaJson, "nome"), Texto(buscaJson, "empresa"));
                mensagens.Add(new ChatMessageItem("assistant", content));
                mensagens.Add(new ChatMessageItem("user", await ResultadoBuscaAsync(criterio, buscar, encontrados)));
                continue;
            }

            return await ProcessarRespostaAsync(root, hoje, rascunho, encontrados, buscar, avisos);
        }

        return new AssistantTurn("Não consegui localizar as informações necessárias. Pode dar mais detalhes?", null, avisos, rascunho);
    }

    // Devolve null quando a resposta não é JSON.
    private JsonDocument? AbrirJson(string content)
    {
        try
        {
            return JsonDocument.Parse(LlmJson.ExtractObject(content));
        }
        catch (JsonException)
        {
            // Registra só o início da resposta para diagnóstico, sem o texto completo do usuário.
            logger.LogWarning("Resposta do assistente sem JSON; tratada como texto: {Trecho}", content.Length > 300 ? content[..300] : content);
            return null;
        }
    }

    private static async Task<string> ResultadoBuscaAsync(
        LeadSearchCriteria criterio,
        Func<LeadSearchCriteria, Task<IReadOnlyList<LeadCandidate>>> buscar,
        Dictionary<Guid, string> encontrados)
    {
        if (criterio.Nome is null && criterio.Empresa is null)
            return "RESULTADO DA BUSCA: nenhum critério informado. Pergunte ao usuário qual lead ele quer.";

        var candidatos = await buscar(criterio);
        foreach (var candidato in candidatos) encontrados[candidato.Id] = candidato.Nome;

        if (candidatos.Count == 0)
            return "RESULTADO DA BUSCA: nenhum lead encontrado. Pergunte ao usuário se é um lead novo ou se o nome está diferente.";

        return $"RESULTADO DA BUSCA: {JsonSerializer.Serialize(candidatos, JsonOptions)}";
    }

    private async Task<AssistantTurn> ProcessarRespostaAsync(
        JsonElement root,
        DateOnly hoje,
        LeadDraft rascunho,
        Dictionary<Guid, string> encontrados,
        Func<LeadSearchCriteria, Task<IReadOnlyList<LeadCandidate>>> buscar,
        List<string> avisos)
    {
        var resposta = Texto(root, "resposta") ?? "Não consegui montar uma resposta.";
        var pronto = root.TryGetProperty("pronto", out var prontoJson) && prontoJson.ValueKind == JsonValueKind.True;

        var parcial = root.TryGetProperty("acao", out var acaoJson) && acaoJson.ValueKind == JsonValueKind.Object
            ? LerAcao(acaoJson, hoje, rascunho, encontrados, avisos)
            : new Parcial(null, null, null, new LeadPatch(), null);

        // Diagnóstico: mostra a ação recebida, sem o texto completo.
        var bruto = root.TryGetProperty("acao", out var acaoBruta) ? acaoBruta.GetRawText() : "(sem acao)";
        logger.LogInformation("Ação recebida (pronto={Pronto}): {Acao}", pronto, bruto.Length > 400 ? bruto[..400] : bruto);

        var novoRascunho = rascunho.Mesclar(parcial.Tipo, parcial.LeadId, parcial.LeadNome, parcial.Campos, parcial.Interacao);

        // Enquanto o modelo não diz "pronto", o rascunho é guardado e nenhuma proposta é criada.
        if (!pronto) return new AssistantTurn(resposta, null, avisos, novoRascunho);

        var acao = await ValidarPropostaAsync(novoRascunho, buscar, hoje, avisos);
        return new AssistantTurn(resposta, acao, avisos, novoRascunho);
    }

    private static Parcial LerAcao(
        JsonElement acaoJson,
        DateOnly hoje,
        LeadDraft rascunho,
        Dictionary<Guid, string> encontrados,
        List<string> avisos)
    {
        var tipo = Texto(acaoJson, "tipo")?.ToLowerInvariant() switch
        {
            "criar" => (LeadActionType?)LeadActionType.Criar,
            "atualizar" => LeadActionType.Atualizar,
            _ => null
        };
        if (tipo is null) return new Parcial(null, null, null, new LeadPatch(), null);

        var campos = acaoJson.TryGetProperty("campos", out var camposJson) && camposJson.ValueKind == JsonValueKind.Object
            ? camposJson
            : default;
        var patch = ParsePatch(campos, hoje, avisos);
        var interacao = Limitar(Texto(acaoJson, "interacao"), MaxInteracao, avisos, "Interação");

        Guid? leadId = null;
        string? leadNome = null;
        if (tipo == LeadActionType.Atualizar)
        {
            var texto = Texto(acaoJson, "leadId");
            // Só aceita ids vistos em uma busca desta conversa ou já selecionados no rascunho.
            if (Guid.TryParse(texto, out var id) && (encontrados.ContainsKey(id) || rascunho.LeadId == id))
            {
                leadId = id;
                leadNome = encontrados.TryGetValue(id, out var nome) ? nome : null;
            }
            else if (texto is not null)
            {
                avisos.Add("Não consegui identificar o lead com segurança.");
            }
        }

        return new Parcial(tipo, leadId, leadNome, patch, interacao);
    }

    // Só vira proposta quando o rascunho está completo. Cada motivo de recusa vira um aviso para o usuário.
    private async Task<LeadAction?> ValidarPropostaAsync(
        LeadDraft draft,
        Func<LeadSearchCriteria, Task<IReadOnlyList<LeadCandidate>>> buscar,
        DateOnly hoje,
        List<string> avisos)
    {
        if (draft.Tipo is null) return null;

        if (draft.Tipo == LeadActionType.Criar)
        {
            if (string.IsNullOrWhiteSpace(draft.Campos.Nome))
            {
                avisos.Add("Ainda falta o nome do lead para criar o cadastro.");
                return null;
            }

            // Alerta antes de criar: se já existe um lead muito parecido, o usuário decide.
            var similares = await buscar(new LeadSearchCriteria(draft.Campos.Nome, draft.Campos.Empresa));
            if (similares.FirstOrDefault(c => c.Score >= LimiteDuplicado) is { } parecido)
            {
                var empresa = parecido.Empresa is null ? string.Empty : $" ({parecido.Empresa})";
                avisos.Add($"Já existe um lead parecido: {parecido.Nome}{empresa}. Se for a mesma pessoa, atualize o lead em vez de criar outro.");
            }

            var camposCriar = SugerirFollowUp(draft.Campos, draft.Campos.Status ?? LeadValues.StatusNovo, null, hoje, avisos);
            return new LeadAction(LeadActionType.Criar, null, camposCriar, draft.Interacao);
        }

        if (draft.LeadId is null)
        {
            avisos.Add("Ainda não identifiquei o lead. Nenhuma alteração foi proposta.");
            return null;
        }

        if (draft.Campos.IsEmpty && draft.Interacao is null)
        {
            avisos.Add("Não há nenhuma alteração para propor.");
            return null;
        }

        // Sugere follow-up só quando a interação ou o status mudou, e sem sobrescrever um follow-up futuro.
        var lead = await leadService.GetAsync(draft.LeadId.Value);
        var camposAtualizar = draft.Campos;
        if (lead is not null && (draft.Interacao is not null || draft.Campos.Status is not null))
        {
            camposAtualizar = SugerirFollowUp(draft.Campos, draft.Campos.Status ?? lead.Status, lead.ProximoFollowUp, hoje, avisos);
        }

        return new LeadAction(LeadActionType.Atualizar, draft.LeadId, camposAtualizar, draft.Interacao);
    }

    // Preenche o próximo follow-up com a cadência do status, quando o usuário não informou uma data.
    private static LeadPatch SugerirFollowUp(LeadPatch campos, string status, DateOnly? followUpAtual, DateOnly hoje, List<string> avisos)
    {
        if (campos.ProximoFollowUp is not null) return campos;
        if (FollowUpPolicy.DiasSugeridos(status) is not { } dias) return campos;
        if (followUpAtual is { } atual && atual >= hoje) return campos;

        var sugestao = hoje.AddDays(dias);
        avisos.Add($"Sugeri follow-up em {sugestao:dd/MM/yyyy} com base no status \"{status}\". Diga se prefere outra data.");
        return campos.Mesclar(new LeadPatch { ProximoFollowUp = sugestao.ToString(FormatoData, CultureInfo.InvariantCulture) });
    }

    private static LeadPatch ParsePatch(JsonElement campos, DateOnly hoje, List<string> avisos)
    {
        if (campos.ValueKind != JsonValueKind.Object) return new LeadPatch();

        return new LeadPatch
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
            ProximoFollowUp = Data(campos, "proximoFollowUp", hoje, avisos),
        };
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

    // Aceita data fixa ou expressão relativa ("amanhã", "sexta", "em 3 dias"); o cálculo é feito aqui, não pelo modelo.
    private static string? Data(JsonElement obj, string propriedade, DateOnly hoje, List<string> avisos)
    {
        var valor = Texto(obj, propriedade);
        if (valor is null) return null;

        if (DataRelativa.Interpretar(valor, hoje) is { } data)
            return data.ToString(FormatoData, CultureInfo.InvariantCulture);

        avisos.Add($"Não entendi a data \"{valor}\". O follow-up foi ignorado; informe uma data como 15/01 ou \"em 3 dias\".");
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
