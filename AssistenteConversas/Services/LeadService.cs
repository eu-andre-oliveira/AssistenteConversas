using AssistenteConversas.Data;
using AssistenteConversas.Models;
using Microsoft.EntityFrameworkCore;

namespace AssistenteConversas.Services;

public sealed class LeadService(IDbContextFactory<AppDbContext> dbFactory) : ILeadService
{
    public async Task<IReadOnlyList<Lead>> SearchAsync(string? name, string? company, string? temperature, string? status)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.Leads.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => x.Nome.Contains(name));
        if (!string.IsNullOrWhiteSpace(company)) query = query.Where(x => x.Empresa != null && x.Empresa.Contains(company));
        if (!string.IsNullOrWhiteSpace(temperature)) query = query.Where(x => x.Temperatura == temperature);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);

        return await query.OrderByDescending(x => x.DataCriacao).ToListAsync();
    }

    public async Task<Lead?> GetAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Leads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Lead> CreateAsync(Lead lead)
    {
        NormalizeAndValidate(lead);
        await using var db = await dbFactory.CreateDbContextAsync();
        lead.Id = lead.Id == Guid.Empty ? Guid.NewGuid() : lead.Id;
        lead.DataCriacao = DateTime.UtcNow;
        lead.DataAtualizacao = lead.DataCriacao;
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        return lead;
    }

    public async Task<bool> UpdateAsync(Lead lead)
    {
        NormalizeAndValidate(lead);
        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.Leads.FirstOrDefaultAsync(x => x.Id == lead.Id);
        if (existing is null) return false;

        lead.DataCriacao = existing.DataCriacao;
        lead.DataAtualizacao = DateTime.UtcNow;
        db.Entry(existing).CurrentValues.SetValues(lead);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var lead = await db.Leads.FindAsync(id);
        if (lead is null) return false;
        // Remove as interações junto com o lead, sem depender de cascade do SQLite.
        await db.Interacoes.Where(x => x.LeadId == id).ExecuteDeleteAsync();
        db.Leads.Remove(lead);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<LeadInteracao>> ListInteracoesAsync(Guid leadId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Interacoes.AsNoTracking()
            .Where(x => x.LeadId == leadId)
            .OrderByDescending(x => x.DataRegistro)
            .ToListAsync();
    }

    public async Task AddInteracaoAsync(Guid leadId, string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) throw new ArgumentException("A interação não pode estar vazia.");
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Leads.AnyAsync(x => x.Id == leadId)) throw new ArgumentException("Lead não encontrado.");
        db.Interacoes.Add(new LeadInteracao { LeadId = leadId, Texto = texto.Trim(), DataRegistro = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    private static void NormalizeAndValidate(Lead lead)
    {
        lead.Nome = lead.Nome.Trim();
        if (string.IsNullOrWhiteSpace(lead.Nome)) throw new ArgumentException("O nome do lead é obrigatório.");
        if (!LeadValues.Temperaturas.Contains(lead.Temperatura)) lead.Temperatura = LeadValues.TemperaturaIndefinido;
        if (!LeadValues.Statuses.Contains(lead.Status)) lead.Status = LeadValues.StatusNovo;
    }
}
