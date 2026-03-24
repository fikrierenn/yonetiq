using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Not asistanıyla çok-turlu sohbet servisi.
/// Gemini API kullanarak konuşma geçmişi ile bağlamsal AI yanıtları üretir.
/// </summary>
public class NoteChatService(HttpClient http, IConfiguration config, ILogger<NoteChatService> logger)
{
    private readonly string _apiKey = config["AI:Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
    private readonly string _endpoint = (config["AI:Gemini:Endpoint"] ?? "https://generativelanguage.googleapis.com").TrimEnd('/');
    private readonly string[] _models = BuildModelChain(config["AI:Gemini:Model"] ?? Environment.GetEnvironmentVariable("AI__Gemini__Model"));

    private static string[] BuildModelChain(string? preferred)
    {
        var all = new[]
        {
            "gemini-2.5-flash",
            "gemini-2.5-pro",
            "gemini-3.1-flash-lite-preview",
            "gemini-2.0-flash",
            "gemini-1.5-flash",
            "gemini-2.5-flash-lite",
        };
        if (string.IsNullOrWhiteSpace(preferred)) return all;
        return new[] { preferred }.Concat(all.Where(m => m != preferred)).ToArray();
    }

    /// <summary>
    /// Not asistanıyla çok-turlu sohbet. Geçmiş konuşma bağlamını Gemini'ya gönderir.
    /// </summary>
    public async Task<ServiceResult<string>> ChatAsync(
        List<(string Role, string Text)> history,
        string userMessage)
    {
        const string systemInstruction = """
            Sen bir kişisel not asistanısın. Kullanıcıların düşüncelerini, fikirlerini ve planlarını düzenli notlara dönüştürmesine yardım ediyorsun.

            Davranış kuralların:
            - Türkçe konuş, samimi ve yardımsever ol
            - Kullanıcının anlatmak istediğini tam anlamak için kısa ve net sorular sor
            - Her turda en fazla 1-2 soru sor
            - Notu genişletip yapılandırmak için öneriler sun
            - Not aksiyon gerektiriyorsa bunu belirt
            - Not yeterince dolduğunda özet verip kaydetmeyi öner

            Notu kaydetmeye hazır olduğunda YANIT SONUNA şu bloğu EKSİKSİZ ekle:
            ---NOT HAZIR---
            Başlık: [not başlığı]
            İçerik: [genişletilmiş, iyi yapılandırılmış not içeriği]
            Etiketler: [virgülle ayrılmış etiketler, en fazla 5]
            ---SON---
            """;

        var allContents = new List<(string Role, string Text)>(history)
        {
            ("user", userMessage)
        };

        var aiResponse = await TryChatWithGeminiAsync(systemInstruction, allContents);
        if (aiResponse is not null)
            return ServiceResult<string>.Success(aiResponse);

        return ServiceResult<string>.Failure("AI asistana şu an ulaşılamıyor.", "AiUnavailable");
    }

    private async Task<string?> TryChatWithGeminiAsync(string systemInstruction, List<(string Role, string Text)> contents)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("Gemini API Key is missing.");
            return null;
        }

        var jsonPayload = JsonSerializer.Serialize(new
        {
            system_instruction = new { parts = new[] { new { text = systemInstruction } } },
            contents = contents.Select(c => new
            {
                role = c.Role,
                parts = new[] { new { text = c.Text } }
            }).ToArray(),
            generationConfig = new { temperature = 0.7 }
        });

        foreach (var model in _models)
        {
            var url = $"{_endpoint}/v1beta/models/{model}:generateContent";
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Add("x-goog-api-key", _apiKey);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                req.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var res = await http.SendAsync(req);
                if ((int)res.StatusCode == 429 || (int)res.StatusCode >= 500)
                {
                    logger.LogWarning("[NoteChat] Model {Model} → {Status}, trying next...", model, res.StatusCode);
                    await Task.Delay(1000);
                    continue;
                }
                if (!res.IsSuccessStatusCode) continue;

                await using var stream = await res.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[NoteChat] Model {Model} failed, trying next...", model);
            }
        }
        return null;
    }
}
