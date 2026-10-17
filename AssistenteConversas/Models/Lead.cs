namespace AssistenteConversas.Models;

public sealed class Lead
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? Cargo { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? PerfilLinkedIn { get; set; }
    public string? Necessidade { get; set; }
    public string? ProdutoInteresse { get; set; }
    public string? NivelInteresse { get; set; }
    public string Temperatura { get; set; } = LeadValues.TemperaturaIndefinido;
    public string Status { get; set; } = LeadValues.StatusNovo;
    public string? PrazoEstimado { get; set; }
    public string? ProximaAcao { get; set; }
    public DateOnly? ProximoFollowUp { get; set; }
    public string? Resumo { get; set; }
    public string? Observacoes { get; set; }
    public string ConversaOriginal { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
}

public static class LeadValues
{
    public const string TemperaturaFrio = "Frio";
    public const string TemperaturaMorno = "Morno";
    public const string TemperaturaQuente = "Quente";
    public const string TemperaturaIndefinido = "Indefinido";

    public const string StatusNovo = "Novo";
    public const string StatusEmAnalise = "Em análise";
    public const string StatusContatoRealizado = "Contato realizado";
    public const string StatusReuniaoAgendada = "Reunião agendada";
    public const string StatusPropostaEnviada = "Proposta enviada";
    public const string StatusConvertido = "Convertido";
    public const string StatusPerdido = "Perdido";
    public const string StatusArquivado = "Arquivado";

    public static readonly IReadOnlySet<string> Temperaturas = new HashSet<string>(StringComparer.Ordinal)
    {
        TemperaturaFrio, TemperaturaMorno, TemperaturaQuente, TemperaturaIndefinido
    };

    public static readonly IReadOnlySet<string> Statuses = new HashSet<string>(StringComparer.Ordinal)
    {
        StatusNovo, StatusEmAnalise, StatusContatoRealizado, StatusReuniaoAgendada,
        StatusPropostaEnviada, StatusConvertido, StatusPerdido, StatusArquivado
    };
}
