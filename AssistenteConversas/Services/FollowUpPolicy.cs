using AssistenteConversas.Models;

namespace AssistenteConversas.Services;

// Regra simples de cadência: quantos dias depois de um status o próximo contato costuma ser retomado.
// Status sem valor (Convertido, Perdido, Arquivado) não geram sugestão.
public static class FollowUpPolicy
{
    private static readonly IReadOnlyDictionary<string, int> DiasPorStatus = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [LeadValues.StatusNovo] = 2,
        [LeadValues.StatusEmAnalise] = 3,
        [LeadValues.StatusContatoRealizado] = 3,
        [LeadValues.StatusReuniaoAgendada] = 1,
        [LeadValues.StatusPropostaEnviada] = 3,
    };

    public static int? DiasSugeridos(string status) =>
        DiasPorStatus.TryGetValue(status, out var dias) ? dias : null;
}
