using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

/// <summary>
/// MailKit kullanarak e-posta gönderimini yöneten servis.
/// SMTP ayarları SettingsService üzerinden DB'den okunur (appsettings.json değil).
/// </summary>
public class EmailService(SettingsService settings, ILogger<EmailService> logger)
{
    /// <summary>
    /// Belirtilen alıcıya HTML formatında e-posta gönderir.
    /// </summary>
    public async Task<ServiceResult> SendAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var cfg = await settings.GetGroupAsync("Email");
            if (!TryBuildClient(cfg, out var client, out var error))
                return ServiceResult.Failure(error!);

            var message = BuildMessage(cfg, to, subject, htmlBody);
            using (client!)
            {
                await SendAndDisconnect(client!, message);
            }
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "E-posta gönderilemedi: {To}", to);
            return ServiceResult.Failure($"E-posta gönderilemedi: {ex.Message}");
        }
    }

    /// <summary>
    /// Şifre sıfırlama e-postası gönderir.
    /// </summary>
    public async Task<ServiceResult> SendPasswordResetAsync(string to, string fullName, string resetLink)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto">
              <h2 style="color:#1a7f8e">Şifre Sıfırlama</h2>
              <p>Merhaba <strong>{fullName}</strong>,</p>
              <p>YonetIQ şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın:</p>
              <p style="text-align:center;margin:2rem 0">
                <a href="{resetLink}" style="background:#1a7f8e;color:#fff;padding:12px 28px;
                   border-radius:6px;text-decoration:none;font-weight:600">Şifremi Sıfırla</a>
              </p>
              <p style="color:#6c757d;font-size:0.85rem">
                Bu bağlantı 2 saat geçerlidir. Eğer bu isteği siz yapmadıysanız bu e-postayı dikkate almayın.
              </p>
              <hr style="border:none;border-top:1px solid #dee2e6"/>
              <p style="color:#6c757d;font-size:0.8rem">YonetIQ Kurumsal Yönetim Portali</p>
            </div>
            """;
        return await SendAsync(to, "YonetIQ — Şifre Sıfırlama", html);
    }

    /// <summary>
    /// Rapor sonucunu HTML tablo formatında e-posta olarak gönderir.
    /// </summary>
    public async Task<ServiceResult> SendReportAsync(string to, string reportName, QueryResult result, string? aiSummary = null)
    {
        var html = BuildReportHtml(reportName, result, aiSummary);
        return await SendAsync(to, $"YonetIQ Rapor: {reportName}", html);
    }

    /// <summary>
    /// SMTP bağlantısını test eder (Ayarlar sayfası için).
    /// </summary>
    public async Task<(bool Ok, string? Error)> TestConnectionAsync()
    {
        try
        {
            var cfg = await settings.GetGroupAsync("Email");
            if (!TryBuildClient(cfg, out var client, out var error))
                return (false, error);

            using (client)
            {
                // Bağlantı kurar ve hemen keser
                await client!.DisconnectAsync(true);
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // ── Yardımcı Metodlar ────────────────────────────────────────────────────

    private static bool TryBuildClient(Dictionary<string, string?> cfg,
        out SmtpClient? client, out string? error)
    {
        client = null; error = null;

        var host = cfg.GetValueOrDefault("Email.Host");
        if (string.IsNullOrWhiteSpace(host))
        {
            error = "SMTP sunucu adresi tanımlanmamış. Lütfen /ayarlar sayfasından yapılandırın.";
            return false;
        }

        if (!int.TryParse(cfg.GetValueOrDefault("Email.Port") ?? "587", out var port))
            port = 587;

        var useSsl = (cfg.GetValueOrDefault("Email.UseSsl") ?? "true").Equals("true", StringComparison.OrdinalIgnoreCase);
        var username = cfg.GetValueOrDefault("Email.Username");
        var password = cfg.GetValueOrDefault("Email.Password");

        var secureOption = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        client = new SmtpClient();
        client.Connect(host, port, secureOption);

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            client.Authenticate(username, password);

        return true;
    }

    private static MimeMessage BuildMessage(Dictionary<string, string?> cfg,
        string to, string subject, string htmlBody)
    {
        var fromAddr = cfg.GetValueOrDefault("Email.FromAddress") ?? string.Empty;
        var fromName = cfg.GetValueOrDefault("Email.FromName") ?? "YonetIQ";

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(fromName, fromAddr));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new TextPart("html") { Text = htmlBody };
        return msg;
    }

    private static async Task SendAndDisconnect(SmtpClient client, MimeMessage message)
    {
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    /// <summary>
    /// Rapor sonucundan inline-CSS HTML tablo üretir.
    /// </summary>
    internal static string BuildReportHtml(string reportName, QueryResult result, string? aiSummary)
    {
        const int maxRows = 500;
        var rows    = result.Rows;
        var columns = result.Columns;
        var rowCount = rows.Count;
        var truncated = rowCount > maxRows;
        var displayRows = truncated ? rows.Take(maxRows).ToList() : rows;

        var sb = new System.Text.StringBuilder();
        sb.Append($"""
            <div style="font-family:Arial,sans-serif;max-width:900px;margin:auto">
              <h2 style="color:#1a7f8e;margin-bottom:4px">{System.Net.WebUtility.HtmlEncode(reportName)}</h2>
              <p style="color:#6c757d;font-size:0.85rem;margin-top:0">
                Çalıştırma tarihi: {DateTime.Now:dd.MM.yyyy HH:mm} &nbsp;|&nbsp;
                Toplam satır: <strong>{rowCount:N0}</strong>
              </p>
            """);

        // AI özeti kutusu
        if (!string.IsNullOrWhiteSpace(aiSummary))
        {
            sb.Append($"""
              <div style="background:#fff3cd;border:1px solid #ffc107;border-radius:6px;padding:12px 16px;margin-bottom:16px">
                <strong>🤖 AI Yorumu</strong>
                <p style="margin:6px 0 0">{System.Net.WebUtility.HtmlEncode(aiSummary)}</p>
              </div>
            """);
        }

        // Tablo başlığı
        sb.Append("""
              <table style="border-collapse:collapse;width:100%;font-size:0.85rem">
                <thead>
                  <tr style="background:#1a7f8e;color:#fff">
            """);
        foreach (var col in columns)
            sb.Append($"      <th style=\"padding:8px 10px;text-align:left\">{System.Net.WebUtility.HtmlEncode(col)}</th>");
        sb.Append("  </tr></thead><tbody>");

        // Satırlar
        var isOdd = false;
        foreach (var row in displayRows)
        {
            var bg = isOdd ? "#f8f9fa" : "#fff";
            isOdd = !isOdd;
            sb.Append($"<tr style=\"background:{bg}\">");
            foreach (var col in columns)
            {
                var val = row.TryGetValue(col, out var v) ? v?.ToString() ?? "" : "";
                sb.Append($"<td style=\"padding:6px 10px;border-bottom:1px solid #dee2e6\">{System.Net.WebUtility.HtmlEncode(val)}</td>");
            }
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");

        if (truncated)
            sb.Append($"<p style=\"color:#6c757d;font-size:0.8rem\">... ve {rowCount - maxRows:N0} satır daha (e-postada gösterilmedi).</p>");

        sb.Append("<hr style=\"border:none;border-top:1px solid #dee2e6;margin-top:24px\"/>");
        sb.Append("<p style=\"color:#6c757d;font-size:0.8rem\">YonetIQ Kurumsal Yönetim Portali</p>");
        sb.Append("</div>");

        return sb.ToString();
    }
}
