using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Proaktif insight'lar üretir — deterministik iş kurallarıyla, AI çağrısı yapmadan.
/// Dashboard'da kullanıcıya dikkat gerektiren alanları gösterir.
/// </summary>
public class ProactiveInsightService(
    TaskService taskSvc,
    MeetingService meetingSvc,
    ApprovalService approvalSvc,
    OkrService okrSvc,
    ILogger<ProactiveInsightService> logger)
{
    public async Task<List<AiInsight>> GenerateInsightsAsync(int userId)
    {
        var insights = new List<AiInsight>();

        try
        {
            // 1. Geciken görevler
            var overdueResult = await taskSvc.GetOverdueTaskCountAsync();
            if (overdueResult is { IsSuccess: true, Data: > 0 })
            {
                insights.Add(new AiInsight
                {
                    Type = InsightType.OverdueTasks,
                    Priority = overdueResult.Data >= 5 ? InsightPriority.Critical : InsightPriority.High,
                    Title = $"{overdueResult.Data} görev gecikmiş",
                    Detail = "Son tarihi geçmiş tamamlanmamış görevler mevcut. Önceliklendirme önerilir.",
                    Module = "task",
                    NavigateUrl = "/gorevler",
                    Icon = "bi-exclamation-triangle-fill",
                    IconColor = "text-danger",
                    NumericValue = overdueResult.Data
                });
            }

            // 2. Bugün son tarihli görevler
            var dueTodayResult = await taskSvc.GetDueTodayCountAsync();
            if (dueTodayResult is { IsSuccess: true, Data: > 0 })
            {
                insights.Add(new AiInsight
                {
                    Type = InsightType.Risk,
                    Priority = InsightPriority.High,
                    Title = $"{dueTodayResult.Data} görevin bugün son tarihi",
                    Detail = "Bugün teslim edilmesi gereken görevler var.",
                    Module = "task",
                    NavigateUrl = "/gorevler",
                    Icon = "bi-clock-fill",
                    IconColor = "text-warning",
                    NumericValue = dueTodayResult.Data
                });
            }

            // 3. Bekleyen kararlar
            var decisionsResult = await meetingSvc.GetPendingDecisionCountAsync();
            if (decisionsResult is { IsSuccess: true, Data: > 0 })
            {
                insights.Add(new AiInsight
                {
                    Type = InsightType.PendingDecisions,
                    Priority = decisionsResult.Data >= 5 ? InsightPriority.High : InsightPriority.Normal,
                    Title = $"{decisionsResult.Data} karar bekliyor",
                    Detail = "Toplantılardan çıkan kararlar henüz sonuçlandırılmamış.",
                    Module = "meeting",
                    NavigateUrl = "/toplantilar",
                    Icon = "bi-chat-left-dots-fill",
                    IconColor = "text-info",
                    NumericValue = decisionsResult.Data
                });
            }

            // 4. Bekleyen onaylar
            var approvalResult = await approvalSvc.GetPendingApprovalCountAsync(userId);
            if (approvalResult is { IsSuccess: true, Data: > 0 })
            {
                insights.Add(new AiInsight
                {
                    Type = InsightType.PendingApprovals,
                    Priority = InsightPriority.High,
                    Title = $"{approvalResult.Data} onay bekliyor",
                    Detail = "Sizin onayınızı bekleyen talepler var.",
                    Module = "approval",
                    NavigateUrl = "/onaylar",
                    Icon = "bi-check-circle-fill",
                    IconColor = "text-primary",
                    NumericValue = approvalResult.Data
                });
            }

            // 5. Bugünkü toplantılar
            var meetingsResult = await meetingSvc.GetTodayMeetingsAsync();
            if (meetingsResult is { IsSuccess: true, Data.Count: > 0 })
            {
                var nextMeeting = meetingsResult.Data.FirstOrDefault(m => m.MeetingDate > DateTime.Now);
                if (nextMeeting is not null)
                {
                    insights.Add(new AiInsight
                    {
                        Type = InsightType.UpcomingMeetings,
                        Priority = InsightPriority.Normal,
                        Title = $"Sıradaki toplantı: {nextMeeting.Title}",
                        Detail = $"Saat {nextMeeting.MeetingDate:HH:mm} — {nextMeeting.Location ?? "Konum belirtilmemiş"}",
                        Module = "meeting",
                        NavigateUrl = $"/toplanti/duzenle/{nextMeeting.Id}",
                        Icon = "bi-calendar-event-fill",
                        IconColor = "text-success",
                        NumericValue = meetingsResult.Data.Count
                    });
                }
            }

            // 6. Dün tamamlanan görevler (pozitif insight)
            var completedResult = await taskSvc.GetCompletedLast24hCountAsync();
            if (completedResult is { IsSuccess: true, Data: > 0 })
            {
                insights.Add(new AiInsight
                {
                    Type = InsightType.CompletedYesterday,
                    Priority = InsightPriority.Low,
                    Title = $"Son 24 saatte {completedResult.Data} görev tamamlandı",
                    Detail = "Ekip üretkenliği devam ediyor.",
                    Module = "task",
                    Icon = "bi-check2-all",
                    IconColor = "text-success",
                    NumericValue = completedResult.Data
                });
            }

            // 7. OKR ilerleme kontrolü (çeyrek bazlı)
            var currentQuarter = $"Q{(DateTime.Now.Month - 1) / 3 + 1}";
            var okrResult = await okrSvc.ListAsync(year: DateTime.Now.Year, period: currentQuarter);
            if (okrResult is { IsSuccess: true, Data: not null })
            {
                var behindObjectives = okrResult.Data.Where(o => o.Progress < 25 && o.Status == "Active").ToList();
                if (behindObjectives.Count > 0)
                {
                    insights.Add(new AiInsight
                    {
                        Type = InsightType.Risk,
                        Priority = InsightPriority.Normal,
                        Title = $"{behindObjectives.Count} OKR hedefi geride",
                        Detail = "Bu çeyrek için ilerleme düşük olan hedefler var.",
                        Module = "okr",
                        NavigateUrl = "/hedefler",
                        Icon = "bi-bullseye",
                        IconColor = "text-warning",
                        NumericValue = behindObjectives.Count
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating proactive insights");
        }

        // Priority sırasına göre sırala
        return insights.OrderByDescending(i => i.Priority).ThenBy(i => i.Type).ToList();
    }
}
