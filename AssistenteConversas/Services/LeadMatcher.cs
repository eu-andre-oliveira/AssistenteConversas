using System.Globalization;
using System.Text;
using AssistenteConversas.Models;

namespace AssistenteConversas.Services;

// Localiza leads por nome e empresa, tolerando acento, caixa, espaços e pequenos erros de digitação.
public sealed class LeadMatcher
{
    private const double LimiteMinimo = 0.6;
    private const int MaximoResultados = 5;

    public IReadOnlyList<LeadCandidate> Buscar(IEnumerable<Lead> leads, LeadSearchCriteria criterio)
    {
        var nome = Normalizar(criterio.Nome);
        var empresa = Normalizar(criterio.Empresa);
        if (nome.Length == 0 && empresa.Length == 0) return [];

        return leads
            .Select(lead => (Lead: lead, Score: Pontuar(lead, nome, empresa)))
            .Where(item => item.Score >= LimiteMinimo)
            .OrderByDescending(item => item.Score)
            .Take(MaximoResultados)
            .Select(item => new LeadCandidate(
                item.Lead.Id,
                item.Lead.Nome,
                item.Lead.Empresa,
                item.Lead.Cargo,
                item.Lead.Status,
                item.Lead.Temperatura,
                item.Lead.ProximoFollowUp,
                Math.Round(item.Score, 2)))
            .ToList();
    }

    private static double Pontuar(Lead lead, string nome, string empresa)
    {
        if (nome.Length == 0) return Similaridade(Normalizar(lead.Empresa), empresa);

        var pontuacaoNome = Similaridade(Normalizar(lead.Nome), nome);
        if (empresa.Length == 0) return pontuacaoNome;

        return 0.7 * pontuacaoNome + 0.3 * Similaridade(Normalizar(lead.Empresa), empresa);
    }

    private static double Similaridade(string texto, string termo)
    {
        if (texto.Length == 0 || termo.Length == 0) return 0;
        if (texto == termo) return 1;
        if (texto.Contains(termo, StringComparison.Ordinal)) return 0.9;

        var tokensTexto = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tokensTermo = termo.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokensTermo.All(token => tokensTexto.Contains(token))) return 0.85;

        var distancia = DistanciaLevenshtein(texto, termo);
        return 1.0 - (double)distancia / Math.Max(texto.Length, termo.Length);
    }

    // Remove acentos, passa para minúsculas e normaliza espaços.
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return string.Empty;

        var semAcento = new StringBuilder();
        foreach (var caractere in valor.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                semAcento.Append(caractere);
        }

        var texto = semAcento.ToString().ToLowerInvariant();
        return string.Join(' ', texto.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static int DistanciaLevenshtein(string a, string b)
    {
        var anterior = new int[b.Length + 1];
        var atual = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) anterior[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            atual[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var custo = a[i - 1] == b[j - 1] ? 0 : 1;
                atual[j] = Math.Min(Math.Min(atual[j - 1] + 1, anterior[j] + 1), anterior[j - 1] + custo);
            }
            (anterior, atual) = (atual, anterior);
        }

        return anterior[b.Length];
    }
}
