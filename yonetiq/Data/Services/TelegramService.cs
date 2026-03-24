using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

/// <summary>
/// Telegram Bot API üzerinden mesaj gönderimini yöneten servis.
/// Bot token SettingsService'ten okunur (appsettings.json değil).
/// </summary>
public class TelegramService(SettingsService settings, HttpClient http, ILogger<TelegramService> logger)
{
    private const int MaxTableRows = 20;
    private const int TelegramMaxChars = 4000; // Telegram 4096 limit, biraz düşük tutuyoruz

    /// <summary>
    /// Belirtilen Chat ID'ye Markdown formatında mesaj gönderir.
    /// </summary>
    public async Task<ServiceResult> SendMessageAsync(string chatId, string text)
    {
        try
        {
            var token = await settings.GetAsync("Telegram.BotToken");
            if (string.IsNullOrWhiteSpace(token))
                return ServiceResult.Failure("Telegram bot token tanımlanmamış. Lütfen /ayarlar sayfasından yapılandırın.");

            var url = $"https://api.telegram.org/bot{token}/sendMessage";
            var payload = new { chat_id = chatId, text, parse_mode = "Markdown" };

            var response = await http.PostAsJsonAsync(url, payload);
            if (response.IsSuccessStatusCode)
                return ServiceResult.Success();

            var body = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Telegram API hatası: {Status} {Body}", response.StatusCode, body);
            return ServiceResult.Failure($"Telegram API hatası: {(int)response.StatusCode} — {ExtractTelegramError(body)}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Telegram mesaj gönderilemedi: {ChatId}", chatId);
            return ServiceResult.Failure($"Telegram hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Rapor özetini Telegram mesajı olarak iletir.
    /// </summary>
    public async Task<ServiceResult> SendReportSummaryAsync(string chatId, string reportName, QueryResult result, string? aiSummary = null)
    {
        var text = BuildReportMessage(reportName, result, aiSummary);
        return await SendMessageAsync(chatId, text);
    }

    /// <summary>
    /// Bot token geçerliliğini /getMe endpoint'i ile test eder.
    /// </summary>
    public async Task<(bool Ok, string? BotName, string? Error)> TestBotAsync()
    {
        try
        {
            var token = await settings.GetAsync("Telegram.BotToken");
            if (string.IsNullOrWhiteSpace(token))
                return (false, null, "Bot token tanımlanmamış.");

            var response = await http.GetAsync($"https://api.telegram.org/bot{token}/getMe");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, null, ExtractTelegramError(body));

            using var doc = JsonDocument.Parse(body);
            var botName = doc.RootElement
                .GetProperty("result")
                .GetProperty("first_name")
                .GetString();
            return (true, botName, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Telegram Login Widget verisini doğrular.
    /// Telegram'ın belgelediği HMACSHA256(SHA256(BotToken), data_check_string) yöntemi.
    /// </summary>
    public async Task<bool> VerifyLoginWidgetAsync(Dictionary<string, string> data)
    {
        try
        {
            var token = await settings.GetAsync("Telegram.BotToken");
            if (string.IsNullOrWhiteSpace(token)) return false;

            if (!data.TryGetValue("hash", out var receivedHash)) return false;

            // data_check_string: hash hariç tüm alan=değer çiftleri sıralı
            var checkString = string.Join("\n",
                data.Where(kv => kv.Key != "hash")
                    .OrderBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key}={kv.Value}"));

            // Secret key: SHA256(bot_token)
            var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            var computedHash = Convert.ToHexString(
                HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(checkString))).ToLower();

            if (!computedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase))
                return false;

            // auth_date 24 saatten eski mi?
            if (data.TryGetValue("auth_date", out var authDateStr) &&
                long.TryParse(authDateStr, out var authDate))
            {
                var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - authDate;
                if (age > 86400) return false; // 24 saat
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Telegram widget doğrulama hatası");
            return false;
        }
    }

    // ── Yardımcı Metodlar ────────────────────────────────────────────────────

    private static string BuildReportMessage(string reportName, QueryResult result, string? aiSummary)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📊 *{EscapeMarkdown(reportName)}*");
        sb.AppendLine($"_Çalışma: {DateTime.Now:dd.MM.yyyy HH:mm} | {result.RowCount:N0} satır_");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(aiSummary))
        {
            sb.AppendLine("🤖 *AI Yorumu*");
            sb.AppendLine(EscapeMarkdown(aiSummary.Length > 300 ? aiSummary[..300] + "..." : aiSummary));
            sb.AppendLine();
        }

        if (result.Columns.Count > 0 && result.Rows.Count > 0)
        {
            sb.AppendLine("```");
            // Başlık satırı (ilk 4 kolon, max 15 char truncate)
            var cols = result.Columns.Take(4).ToList();
            sb.AppendLine(string.Join(" | ", cols.Select(c => Truncate(c, 12))));
            sb.AppendLine(new string('-', Math.Min(cols.Count * 15, 60)));

            int rowsPrinted = 0;
            foreach (var row in result.Rows.Take(MaxTableRows))
            {
                var values = cols.Select(c =>
                    Truncate(row.TryGetValue(c, out var v) ? v?.ToString() ?? "" : "", 12));
                var line = string.Join(" | ", values);
                sb.AppendLine(line);
                rowsPrinted++;

                if (sb.Length > TelegramMaxChars - 200)
                {
                    sb.AppendLine($"... ve {result.RowCount - rowsPrinted} satır daha");
                    break;
                }
            }

            if (result.RowCount > MaxTableRows && sb.Length <= TelegramMaxChars - 100)
                sb.AppendLine($"... ve {result.RowCount - MaxTableRows} satır daha");

            sb.AppendLine("```");
        }

        return sb.ToString().TrimEnd();
    }

    private static string EscapeMarkdown(string text) =>
        text.Replace("_", "\\_").Replace("*", "\\*").Replace("`", "\\`").Replace("[", "\\[");

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s.PadRight(max) : s[..(max - 1)] + "…";

    private static string ExtractTelegramError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("description", out var desc)
                ? desc.GetString() ?? body
                : body;
        }
        catch { return body; }
    }
}
