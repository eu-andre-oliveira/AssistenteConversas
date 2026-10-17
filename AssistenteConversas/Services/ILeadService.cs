using AssistenteConversas.Models;

namespace AssistenteConversas.Services;

public interface ILeadService
{
    Task<IReadOnlyList<Lead>> SearchAsync(string? name, string? company, string? temperature, string? status);
    Task<Lead?> GetAsync(Guid id);
    Task<Lead> CreateAsync(Lead lead);
    Task<bool> UpdateAsync(Lead lead);
    Task<bool> DeleteAsync(Guid id);
    Task<IReadOnlyList<LeadInteracao>> ListInteracoesAsync(Guid leadId);
    Task AddInteracaoAsync(Guid leadId, string texto);
}
