namespace AssistenteConversas.Models;

// Campos que o assistente propõe alterar. Todos opcionais: null significa "não mexer".
// Os valores chegam como texto (ex.: data em yyyy-MM-dd) e são validados antes de aplicar.
public sealed class LeadPatch
{
    public string? Nome { get; set; }
    public string? Empresa { get; set; }
    public string? Cargo { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? PerfilLinkedIn { get; set; }
    public string? Necessidade { get; set; }
    public string? ProdutoInteresse { get; set; }
    public string? NivelInteresse { get; set; }
    public string? Temperatura { get; set; }
    public string? Status { get; set; }
    public string? PrazoEstimado { get; set; }
    public string? ProximaAcao { get; set; }
    public string? ProximoFollowUp { get; set; }
    public string? Resumo { get; set; }
    public string? Observacoes { get; set; }

    public bool IsEmpty => GetChanges().Count == 0;

    // Lista (campo, valor) apenas dos campos preenchidos. Usada na tela de confirmação.
    public IReadOnlyList<(string Campo, string Valor)> GetChanges()
    {
        var changes = new List<(string, string)>();
        void Add(string campo, string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor)) changes.Add((campo, valor));
        }

        Add("Nome", Nome);
        Add("Empresa", Empresa);
        Add("Cargo", Cargo);
        Add("E-mail", Email);
        Add("Telefone", Telefone);
        Add("Perfil LinkedIn", PerfilLinkedIn);
        Add("Necessidade", Necessidade);
        Add("Produto de interesse", ProdutoInteresse);
        Add("Nível de interesse", NivelInteresse);
        Add("Temperatura", Temperatura);
        Add("Status", Status);
        Add("Prazo estimado", PrazoEstimado);
        Add("Próxima ação", ProximaAcao);
        Add("Próximo follow-up", ProximoFollowUp);
        Add("Resumo", Resumo);
        Add("Observações", Observacoes);
        return changes;
    }
}
