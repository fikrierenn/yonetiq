namespace YonetIQ.Data.Infrastructure;

/// <summary>
/// Multi-provider AI konfigürasyonu.
/// .env veya appsettings'den okunur. Birincil provider başarısız olursa fallback'e düşer.
/// </summary>
public class AiProviderConfig
{
    /// <summary>Aktif provider: "gemini" (varsayılan), "openai", "anthropic"</summary>
    public string PrimaryProvider { get; set; } = "gemini";

    /// <summary>Fallback provider (opsiyonel)</summary>
    public string? FallbackProvider { get; set; }

    // Gemini
    public string GeminiApiKey { get; set; } = string.Empty;
    public string GeminiEndpoint { get; set; } = "https://generativelanguage.googleapis.com";
    public string GeminiModel { get; set; } = "gemini-2.5-flash";

    // OpenAI (opsiyonel)
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiEndpoint { get; set; } = "https://api.openai.com";
    public string OpenAiModel { get; set; } = "gpt-4o-mini";

    // Anthropic (opsiyonel)
    public string AnthropicApiKey { get; set; } = string.Empty;
    public string AnthropicEndpoint { get; set; } = "https://api.anthropic.com";
    public string AnthropicModel { get; set; } = "claude-sonnet-4-20250514";

    /// <summary>
    /// .env ve IConfiguration'dan provider konfigürasyonunu yükler.
    /// </summary>
    public static AiProviderConfig Load(IConfiguration config)
    {
        return new AiProviderConfig
        {
            PrimaryProvider = config["AI:PrimaryProvider"]
                ?? Environment.GetEnvironmentVariable("AI_PRIMARY_PROVIDER") ?? "gemini",
            FallbackProvider = config["AI:FallbackProvider"]
                ?? Environment.GetEnvironmentVariable("AI_FALLBACK_PROVIDER"),

            GeminiApiKey = config["AI:Gemini:ApiKey"]
                ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty,
            GeminiEndpoint = (config["AI:Gemini:Endpoint"]
                ?? Environment.GetEnvironmentVariable("AI__Gemini__Endpoint")
                ?? "https://generativelanguage.googleapis.com").TrimEnd('/'),
            GeminiModel = config["AI:Gemini:Model"]
                ?? Environment.GetEnvironmentVariable("AI__Gemini__Model") ?? "gemini-2.5-flash",

            OpenAiApiKey = config["AI:OpenAI:ApiKey"]
                ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty,
            OpenAiModel = config["AI:OpenAI:Model"]
                ?? Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini",

            AnthropicApiKey = config["AI:Anthropic:ApiKey"]
                ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? string.Empty,
            AnthropicModel = config["AI:Anthropic:Model"]
                ?? Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? "claude-sonnet-4-20250514",
        };
    }

    /// <summary>Belirtilen provider'ın API key'i mevcut mu?</summary>
    public bool IsProviderAvailable(string provider) => provider.ToLowerInvariant() switch
    {
        "gemini" => !string.IsNullOrWhiteSpace(GeminiApiKey),
        "openai" => !string.IsNullOrWhiteSpace(OpenAiApiKey),
        "anthropic" => !string.IsNullOrWhiteSpace(AnthropicApiKey),
        _ => false
    };
}
