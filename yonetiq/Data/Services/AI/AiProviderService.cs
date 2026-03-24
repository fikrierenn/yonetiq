using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Provider-agnostic AI API çağrı servisi.
/// Gemini (birincil) + OpenAI/Anthropic (fallback) desteği.
/// SkillExecutor bu servisi kullanarak provider bağımsız çalışır.
/// </summary>
public class AiProviderService(HttpClient http, IConfiguration config, ILogger<AiProviderService> logger)
{
    private readonly AiProviderConfig _config = AiProviderConfig.Load(config);

    /// <summary>
    /// System + user prompt ile AI çağrısı yapar.
    /// Birincil provider başarısız olursa fallback provider'a düşer.
    /// </summary>
    public async Task<string?> GenerateAsync(string systemPrompt, string userPrompt, float temperature = 0.2f)
    {
        // Birincil provider
        var result = await CallProviderAsync(_config.PrimaryProvider, systemPrompt, userPrompt, temperature);
        if (result is not null) return result;

        // Fallback provider (yapılandırılmışsa)
        if (!string.IsNullOrWhiteSpace(_config.FallbackProvider) &&
            _config.IsProviderAvailable(_config.FallbackProvider))
        {
            logger.LogWarning("Primary provider {Primary} failed, falling back to {Fallback}",
                _config.PrimaryProvider, _config.FallbackProvider);
            result = await CallProviderAsync(_config.FallbackProvider, systemPrompt, userPrompt, temperature);
            if (result is not null) return result;
        }

        logger.LogError("All AI providers exhausted");
        return null;
    }

    private async Task<string?> CallProviderAsync(string provider, string systemPrompt, string userPrompt, float temperature)
    {
        return provider.ToLowerInvariant() switch
        {
            "gemini" => await CallGeminiAsync(systemPrompt, userPrompt, temperature),
            "openai" => await CallOpenAiAsync(systemPrompt, userPrompt, temperature),
            "anthropic" => await CallAnthropicAsync(systemPrompt, userPrompt, temperature),
            _ => null
        };
    }

    private async Task<string?> CallGeminiAsync(string systemPrompt, string userPrompt, float temperature)
    {
        if (!_config.IsProviderAvailable("gemini")) return null;

        var models = BuildGeminiModelChain(_config.GeminiModel);

        foreach (var model in models)
        {
            var url = $"{_config.GeminiEndpoint}/v1beta/models/{model}:generateContent";
            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                    contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
                    generationConfig = new { temperature }
                });

                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Add("x-goog-api-key", _config.GeminiApiKey);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                using var res = await http.SendAsync(req);

                if ((int)res.StatusCode == 429 || (int)res.StatusCode >= 500)
                {
                    logger.LogWarning("[Gemini] Model {Model} → {Status}, trying next...", model, res.StatusCode);
                    await Task.Delay(1000);
                    continue;
                }
                if (!res.IsSuccessStatusCode) continue;

                return await ExtractGeminiText(res);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Gemini] Model {Model} failed", model);
            }
        }
        return null;
    }

    private async Task<string?> CallOpenAiAsync(string systemPrompt, string userPrompt, float temperature)
    {
        if (!_config.IsProviderAvailable("openai")) return null;

        try
        {
            var url = $"{_config.OpenAiEndpoint}/v1/chat/completions";
            var payload = JsonSerializer.Serialize(new
            {
                model = _config.OpenAiModel,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature,
                max_tokens = 4096
            });

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.OpenAiApiKey);
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var res = await http.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("[OpenAI] {Status}", res.StatusCode);
                return null;
            }

            await using var stream = await res.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[OpenAI] Call failed");
            return null;
        }
    }

    private async Task<string?> CallAnthropicAsync(string systemPrompt, string userPrompt, float temperature)
    {
        if (!_config.IsProviderAvailable("anthropic")) return null;

        try
        {
            var url = $"{_config.AnthropicEndpoint}/v1/messages";
            var payload = JsonSerializer.Serialize(new
            {
                model = _config.AnthropicModel,
                max_tokens = 4096,
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content = userPrompt }
                }
            });

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-api-key", _config.AnthropicApiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var res = await http.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("[Anthropic] {Status}", res.StatusCode);
                return null;
            }

            await using var stream = await res.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Anthropic] Call failed");
            return null;
        }
    }

    private static async Task<string?> ExtractGeminiText(HttpResponseMessage res)
    {
        await using var stream = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return null;
        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.GetArrayLength() == 0)
            return null;
        return parts[0].TryGetProperty("text", out var textEl) ? textEl.GetString() : null;
    }

    private static string[] BuildGeminiModelChain(string preferred)
    {
        var all = new[]
        {
            "gemini-2.5-flash", "gemini-2.5-pro", "gemini-3.1-flash-lite-preview",
            "gemini-2.0-flash", "gemini-1.5-flash", "gemini-2.5-flash-lite"
        };
        return new[] { preferred }.Concat(all.Where(m => m != preferred)).ToArray();
    }
}
