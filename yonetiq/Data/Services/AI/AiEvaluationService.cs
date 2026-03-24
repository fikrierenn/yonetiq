using Dapper;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// AI kalite metriklerini toplar ve dashboard için sunar.
/// SkillRegistry'den tüm skill tanımlarını, AiMemoryService'ten istatistikleri alır.
/// </summary>
public class AiEvaluationService(
    IConfiguration config,
    AuditService? auditService,
    AiMemoryService memoryService,
    SkillRegistry skillRegistry,
    ILogger<AiEvaluationService> logger)
    : BaseService(config, auditService)
{
    public async Task<ServiceResult<AiDashboardModel>> GetDashboardAsync()
    {
        return await ExecuteServiceAsync<AiDashboardModel>(async conn =>
        {
            var model = new AiDashboardModel();

            // Overall stats
            model.TotalInteractions = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM AiInteractions");
            model.TotalFeedback = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM AiFeedback");
            model.AvgLatencyMs = await conn.ExecuteScalarAsync<decimal>(
                "SELECT ISNULL(AVG(CAST(LatencyMs AS DECIMAL)), 0) FROM AiInteractions WHERE IsSuccess = 1");
            model.OverallAcceptanceRate = await conn.ExecuteScalarAsync<decimal>(@"
                SELECT CASE WHEN COUNT(*) = 0 THEN 0
                ELSE CAST(SUM(CASE WHEN WasAccepted = 1 THEN 1 ELSE 0 END) AS DECIMAL) * 100 / COUNT(*) END
                FROM AiFeedback");

            // Per-skill stats
            var skills = skillRegistry.ListAll();
            foreach (var skill in skills)
            {
                var statsResult = await memoryService.GetSkillStatsAsync(skill.Id);
                if (statsResult is { IsSuccess: true, Data: not null })
                {
                    model.SkillStats.Add(new SkillDashboardItem
                    {
                        SkillId = skill.Id,
                        SkillName = skill.Name,
                        Category = skill.Category.ToString(),
                        Module = skill.Module,
                        TotalInvocations = statsResult.Data.TotalInvocations,
                        AvgLatencyMs = statsResult.Data.AvgLatencyMs,
                        AcceptanceRate = statsResult.Data.AcceptanceRate,
                        AvgRating = statsResult.Data.AvgRating
                    });
                }
            }

            // Recent interactions (last 20)
            model.RecentInteractions = (await conn.QueryAsync<AiInteraction>(@"
                SELECT TOP 20 Id, UserId, SkillId, Module, InputSummary, OutputSummary,
                       ConfidenceScore, LatencyMs, TokenCount, IsSuccess, CreatedAt
                FROM AiInteractions
                ORDER BY CreatedAt DESC")).ToList();

            // Recent feedback (last 20)
            model.RecentFeedback = (await conn.QueryAsync<AiFeedbackDetail>(@"
                SELECT TOP 20 f.Id, f.InteractionId, f.UserId, f.Rating, f.WasAccepted, f.WasEdited,
                       f.CorrectionText, f.CreatedAt, i.SkillId, i.InputSummary
                FROM AiFeedback f
                LEFT JOIN AiInteractions i ON f.InteractionId = i.Id
                ORDER BY f.CreatedAt DESC")).ToList();

            logger.LogDebug("AI Dashboard loaded: {Interactions} interactions, {Skills} skills",
                model.TotalInteractions, model.SkillStats.Count);

            return model;
        });
    }
}

// ── Dashboard View Models ────────────────────────────

public class AiDashboardModel
{
    public int TotalInteractions { get; set; }
    public int TotalFeedback { get; set; }
    public decimal AvgLatencyMs { get; set; }
    public decimal OverallAcceptanceRate { get; set; }
    public List<SkillDashboardItem> SkillStats { get; set; } = [];
    public List<AiInteraction> RecentInteractions { get; set; } = [];
    public List<AiFeedbackDetail> RecentFeedback { get; set; } = [];
}

public class SkillDashboardItem
{
    public string SkillId { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public int TotalInvocations { get; set; }
    public decimal AvgLatencyMs { get; set; }
    public decimal AcceptanceRate { get; set; }
    public decimal AvgRating { get; set; }
}

public class AiFeedbackDetail
{
    public int Id { get; set; }
    public int InteractionId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; }
    public bool WasAccepted { get; set; }
    public bool WasEdited { get; set; }
    public string? CorrectionText { get; set; }
    public DateTime CreatedAt { get; set; }
    public string SkillId { get; set; } = string.Empty;
    public string InputSummary { get; set; } = string.Empty;
}
