namespace AssistenteConversas.Models;

// Estado de uma intenção em andamento entre mensagens: o que já foi entendido e o que ainda falta.
// Fica no cliente (página do chat) e volta ao servidor a cada mensagem.
public sealed class LeadDraft
{
    public LeadActionType? Tipo { get; init; }
    public Guid? LeadId { get; init; }
    public string? LeadNome { get; init; }
    public LeadPatch Campos { get; init; } = new();
    public string? Interacao { get; init; }

    public bool IsEmpty => Tipo is null && LeadId is null && Campos.IsEmpty && Interacao is null;

    public LeadDraft Mesclar(LeadActionType? tipo, Guid? leadId, string? leadNome, LeadPatch campos, string? interacao) => new()
    {
        Tipo = tipo ?? Tipo,
        LeadId = leadId ?? LeadId,
        LeadNome = leadNome ?? LeadNome,
        Campos = Campos.Mesclar(campos),
        Interacao = interacao ?? Interacao,
    };

    // Texto legível do rascunho, usado no prompt e na tela.
    public string Descrever()
    {
        if (IsEmpty) return "(nenhum)";

        var linhas = new List<string>();
        if (Tipo is not null) linhas.Add($"tipo: {(Tipo == LeadActionType.Criar ? "criar lead" : "atualizar lead")}");
        if (LeadId is not null) linhas.Add($"lead selecionado: {LeadNome ?? "sem nome"} (id {LeadId})");
        foreach (var (campo, valor) in Campos.GetChanges()) linhas.Add($"{campo}: {valor}");
        if (Interacao is not null) linhas.Add($"interação: {Interacao}");
        return string.Join("\n", linhas);
    }
}
