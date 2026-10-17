using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AssistenteConversas.Services;

// Converte o texto de uma data em DateOnly, aceitando formatos fixos e expressões relativas.
// Assim o cálculo de calendário fica no código, e não no modelo.
public static partial class DataRelativa
{
    private static readonly string[] DiasDaSemana = ["domingo", "segunda", "terca", "quarta", "quinta", "sexta", "sabado"];

    public static DateOnly? Interpretar(string? texto, DateOnly hoje)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var valor = Normalizar(texto);

        if (DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
            return iso;

        if (DateOnly.TryParseExact(valor, ["dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var completa))
            return completa;

        // "dd/MM" usa o próximo ano caso a data já tenha passado neste ano.
        var curta = DataCurta().Match(valor);
        if (curta.Success)
        {
            var dia = int.Parse(curta.Groups["dia"].Value, CultureInfo.InvariantCulture);
            var mes = int.Parse(curta.Groups["mes"].Value, CultureInfo.InvariantCulture);
            var candidata = TryData(hoje.Year, mes, dia);
            if (candidata is { } data)
                return data < hoje ? TryData(hoje.Year + 1, mes, dia) : data;
            return null;
        }

        switch (valor)
        {
            case "hoje": return hoje;
            case "amanha": return hoje.AddDays(1);
            case "depois de amanha": return hoje.AddDays(2);
        }

        var relativa = DiasRelativos().Match(valor);
        if (relativa.Success)
        {
            var quantidade = int.Parse(relativa.Groups["quantidade"].Value, CultureInfo.InvariantCulture);
            var unidade = relativa.Groups["unidade"].Value;
            var dias = unidade.StartsWith("semana", StringComparison.Ordinal) ? quantidade * 7 : quantidade;
            return hoje.AddDays(dias);
        }

        // Dia da semana: a próxima ocorrência a partir de amanhã. "sexta" dita numa sexta vira a sexta seguinte.
        var nomeDia = DiasDaSemana.FirstOrDefault(dia => valor == dia || valor == $"proxima {dia}" || valor == $"na {dia}" || valor == $"no {dia}");
        if (nomeDia is not null)
        {
            var alvo = Array.IndexOf(DiasDaSemana, nomeDia);
            var diferenca = ((alvo - (int)hoje.DayOfWeek) + 7) % 7;
            return hoje.AddDays(diferenca == 0 ? 7 : diferenca);
        }

        return null;
    }

    private static DateOnly? TryData(int ano, int mes, int dia)
    {
        try
        {
            return new DateOnly(ano, mes, dia);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    // Mesma normalização usada na busca de leads: sem acento, minúsculas e espaços simples.
    private static string Normalizar(string texto)
    {
        var sem = new StringBuilder();
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sem.Append(c);
        }
        return string.Join(' ', sem.ToString().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    [GeneratedRegex(@"^(?<dia>\d{1,2})/(?<mes>\d{1,2})$")]
    private static partial Regex DataCurta();

    [GeneratedRegex(@"^(?:em|daqui a|daqui)\s+(?<quantidade>\d{1,3})\s+(?<unidade>dias?|semanas?)$")]
    private static partial Regex DiasRelativos();
}
