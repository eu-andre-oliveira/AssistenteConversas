namespace AssistenteConversas.Models;

public sealed class LeadInteracao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LeadId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;
}
