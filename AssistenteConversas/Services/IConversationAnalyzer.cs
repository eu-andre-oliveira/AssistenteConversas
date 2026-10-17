using AssistenteConversas.Models;

namespace AssistenteConversas.Services;

public interface IConversationAnalyzer
{
    Task<ConversationAnalysisResult> AnalyzeAsync(
        string conversation,
        CancellationToken cancellationToken = default);
}
