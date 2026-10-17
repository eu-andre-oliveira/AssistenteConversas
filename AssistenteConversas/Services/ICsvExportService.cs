using AssistenteConversas.Models;

namespace AssistenteConversas.Services;

public interface ICsvExportService
{
    byte[] Export(IEnumerable<Lead> leads);
}
