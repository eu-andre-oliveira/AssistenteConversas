namespace AssistenteConversas.Models;

public sealed class ConversationAnalysisResult
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
    public string? Temperatura { get; set; } = LeadValues.TemperaturaIndefinido;
    public string? PrazoEstimado { get; set; }
    public string? ProximaAcao { get; set; }
    public string? Resumo { get; set; }
    public string? Observacoes { get; set; }
    public decimal? Confianca { get; set; }
}
