using System.Globalization;
using AssistenteConversas.Models;

namespace AssistenteConversas.Services;

// Único ponto que grava as ações do assistente. Só deve ser chamado após a confirmação do usuário.
public sealed class LeadChangeApplier(ILeadService leadService)
{
    public async Task<Guid> ApplyAsync(LeadAction acao, string textoOrigem)
    {
        if (acao.Tipo == LeadActionType.Criar)
        {
            var lead = new Lead { ConversaOriginal = textoOrigem };
            ApplyPatch(lead, acao.Campos);
            var criado = await leadService.CreateAsync(lead);
            if (acao.Interacao is not null) await leadService.AddInteracaoAsync(criado.Id, acao.Interacao);
            return criado.Id;
        }

        var id = acao.LeadId ?? throw new InvalidOperationException("A ação não informa o lead.");
        var existente = await leadService.GetAsync(id) ?? throw new InvalidOperationException("O lead não existe mais.");
        ApplyPatch(existente, acao.Campos);
        await leadService.UpdateAsync(existente);
        if (acao.Interacao is not null) await leadService.AddInteracaoAsync(id, acao.Interacao);
        return id;
    }

    // Copia apenas os campos preenchidos na proposta; os demais permanecem como estão.
    private static void ApplyPatch(Lead lead, LeadPatch patch)
    {
        lead.Nome = patch.Nome ?? lead.Nome;
        lead.Empresa = patch.Empresa ?? lead.Empresa;
        lead.Cargo = patch.Cargo ?? lead.Cargo;
        lead.Email = patch.Email ?? lead.Email;
        lead.Telefone = patch.Telefone ?? lead.Telefone;
        lead.PerfilLinkedIn = patch.PerfilLinkedIn ?? lead.PerfilLinkedIn;
        lead.Necessidade = patch.Necessidade ?? lead.Necessidade;
        lead.ProdutoInteresse = patch.ProdutoInteresse ?? lead.ProdutoInteresse;
        lead.NivelInteresse = patch.NivelInteresse ?? lead.NivelInteresse;
        lead.Temperatura = patch.Temperatura ?? lead.Temperatura;
        lead.Status = patch.Status ?? lead.Status;
        lead.PrazoEstimado = patch.PrazoEstimado ?? lead.PrazoEstimado;
        lead.ProximaAcao = patch.ProximaAcao ?? lead.ProximaAcao;
        lead.Resumo = patch.Resumo ?? lead.Resumo;
        lead.Observacoes = patch.Observacoes ?? lead.Observacoes;

        if (patch.ProximoFollowUp is not null &&
            DateOnly.TryParseExact(patch.ProximoFollowUp, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
        {
            lead.ProximoFollowUp = data;
        }
    }
}
