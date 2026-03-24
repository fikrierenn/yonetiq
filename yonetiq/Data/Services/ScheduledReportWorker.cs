using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Zamanlanmış rapor tetikleyici — 5 dakikada bir çalışır, vadesi gelen raporları gönderir.
/// </summary>
public class ScheduledReportWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduledReportWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ScheduledReportWorker başladı.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueReportsAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ScheduledReportWorker hata.");
            }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task ProcessDueReportsAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var scheduledSvc = scope.ServiceProvider.GetRequiredService<ScheduledReportService>();
        var querySvc     = scope.ServiceProvider.GetRequiredService<QueryService>();
        var emailSvc     = scope.ServiceProvider.GetRequiredService<EmailService>();
        var telegramSvc  = scope.ServiceProvider.GetRequiredService<TelegramService>();

        var dueResult = await scheduledSvc.GetDueReportsAsync();
        if (!dueResult.IsSuccess || dueResult.Data is null) return;

        foreach (var report in dueResult.Data)
        {
            try
            {
                var runResult = await querySvc.RunQueryAsync(report.QueryRecordId, null);
                if (!runResult.IsSuccess || runResult.Data is null) continue;

                ServiceResult sendResult;
                if (report.Channel == "Telegram")
                    sendResult = await telegramSvc.SendReportSummaryAsync(report.Recipient, report.Title, runResult.Data, null);
                else
                    sendResult = await emailSvc.SendReportAsync(report.Recipient, report.Title, runResult.Data, null);

                if (sendResult.IsSuccess)
                {
                    await scheduledSvc.UpdateAfterRunAsync(report.Id);
                    logger.LogInformation("Zamanlı rapor gönderildi: {Title} → {Recipient}", report.Title, report.Recipient);
                }
                else
                {
                    logger.LogWarning("Rapor gönderilemedi: {Title} — {Error}", report.Title, sendResult.Message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Rapor gönderilemedi: {Title}", report.Title);
            }
        }
    }
}
