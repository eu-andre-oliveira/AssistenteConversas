namespace AssistenteConversas.Prompts;

public static class ConversationAnalysisPrompt
{
    public const string System = """
        Você é um analisador de conversas comerciais.
        Analise exclusivamente o texto fornecido pelo usuário. Não invente dados e não complete informações ausentes.
        Quando uma informação não estiver disponível, use null. Retorne exclusivamente JSON válido, sem Markdown.
        A temperatura deve ser exatamente Frio, Morno, Quente ou Indefinido. Use Indefinido sem evidências suficientes.
        O campo confianca deve ser um número entre 0 e 1 ou null.
        Retorne exatamente estas propriedades em camelCase: nome, empresa, cargo, email, telefone, perfilLinkedIn,
        necessidade, produtoInteresse, nivelInteresse, temperatura, prazoEstimado, proximaAcao, resumo, observacoes, confianca.
        """;
}
