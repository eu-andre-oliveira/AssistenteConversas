namespace AssistenteConversas.Models;

// Configuração da integração. O token fica no user secrets (Telegram:BotToken), nunca no appsettings.
public sealed class TelegramOptions
{
    public bool Enabled { get; set; }
    public string? BotToken { get; set; }
    public long[] AllowedChatIds { get; set; } = [];
    public int PollingTimeoutSeconds { get; set; } = 25;
}

// Envelope padrão das respostas da API do Telegram.
public sealed class TelegramResposta<T>
{
    public bool Ok { get; set; }
    public T? Result { get; set; }
    public string? Description { get; set; }
}

public sealed class TelegramUpdate
{
    public long UpdateId { get; set; }
    public TelegramMensagem? Message { get; set; }
    public TelegramCallbackQuery? CallbackQuery { get; set; }
}

public sealed class TelegramMensagem
{
    public long MessageId { get; set; }
    public TelegramChat Chat { get; set; } = new();
    public string? Text { get; set; }
}

public sealed class TelegramChat
{
    public long Id { get; set; }
}

public sealed class TelegramUsuario
{
    public long Id { get; set; }
}

// Clique em um botão inline. Data é o valor definido no botão (ex.: "confirmar").
public sealed class TelegramCallbackQuery
{
    public string Id { get; set; } = string.Empty;
    public TelegramUsuario From { get; set; } = new();
    public TelegramMensagem? Message { get; set; }
    public string? Data { get; set; }
}

public sealed record BotaoTelegram(string Texto, string Dados);
