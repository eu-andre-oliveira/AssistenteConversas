using System.Globalization;
using System.Text;
using AssistenteConversas.Models;

namespace AssistenteConversas.Services;

public sealed class CsvExportService : ICsvExportService
{
    private static readonly string[] Headers =
    [
        "Id", "Nome", "Empresa", "Cargo", "Email", "Telefone", "PerfilLinkedIn",
        "Necessidade", "ProdutoInteresse", "NivelInteresse", "Temperatura", "Status",
        "PrazoEstimado", "ProximaAcao", "ProximoFollowUp", "Resumo", "Observacoes", "ConversaOriginal",
        "DataCriacao", "DataAtualizacao"
    ];

    public byte[] Export(IEnumerable<Lead> leads)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", Headers.Select(Escape)));
        foreach (var lead in leads)
        {
            var values = new object?[]
            {
                lead.Id, lead.Nome, lead.Empresa, lead.Cargo, lead.Email, lead.Telefone,
                lead.PerfilLinkedIn, lead.Necessidade, lead.ProdutoInteresse, lead.NivelInteresse,
                lead.Temperatura, lead.Status, lead.PrazoEstimado, lead.ProximaAcao,
                lead.ProximoFollowUp?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), lead.Resumo,
                lead.Observacoes, lead.ConversaOriginal, lead.DataCriacao, lead.DataAtualizacao
            };
            builder.AppendLine(string.Join(",", values.Select(value => Escape(value?.ToString()))));
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(builder.ToString());
    }

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
