namespace AssistenteConversas.Services;

internal static class LlmJson
{
    // Extrai o objeto JSON de uma resposta do modelo, mesmo que venha dentro de cercas de Markdown.
    public static string ExtractObject(string content)
    {
        var value = content.Trim();
        if (value.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineEnd = value.IndexOf('\n');
            var lastFence = value.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLineEnd >= 0 && lastFence > firstLineEnd)
                value = value[(firstLineEnd + 1)..lastFence].Trim();
        }

        var firstObject = value.IndexOf('{');
        var lastObject = value.LastIndexOf('}');
        if (firstObject < 0 || lastObject <= firstObject) throw new System.Text.Json.JsonException();
        return value[firstObject..(lastObject + 1)];
    }
}
