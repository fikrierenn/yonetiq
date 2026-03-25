using Dapper;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Öğrenme yaşam döngüsünü yönetir: haftalık decay, pattern re-score, promotion.
/// ScheduledReportWorker veya admin tarafından tetiklenir.
/// </summary>
public class ContinuousLearningService(
    IConfiguration config, AuditService auditSvc,
    LearningSignalService signalSvc,
    ILogger<ContinuousLearningService> logger)
    : BaseService(config, auditSvc)
{
    /// <summary>
    /// Tam öğrenme döngüsü: decay → re-score → promote → cleanup.
    /// Haftalık çağrılması önerilir.
    /// </summary>
    public async Task<ServiceResult<LearningCycleReport>> RunCycleAsync()
    {
        var report = new LearningCycleReport();

        // 1. Decay uygula
        var decayResult = await signalSvc.ApplyDecayAsync();
        if (decayResult.IsSuccess)
            report.DecayedPatterns = decayResult.Data;

        // 2. Oto-onaylanan pattern'leri işaretle
        var promoteResult = await PromoteApprovedPatternsAsync();
        if (promoteResult.IsSuccess)
            report.PromotedPatterns = promoteResult.Data;

        // 3. Çok düşük skorlu pattern'leri temizle
        var cleanupResult = await CleanupRejectedPatternsAsync();
        if (cleanupResult.IsSuccess)
            report.CleanedUpPatterns = cleanupResult.Data;

        logger.LogInformation(
            "Learning cycle completed: Decayed={Decayed}, Promoted={Promoted}, Cleaned={Cleaned}",
            report.DecayedPatterns, report.PromotedPatterns, report.CleanedUpPatterns);

        return ServiceResult<LearningCycleReport>.Success(report);
    }

    /// <summary>ConfidenceScore ≥ 8 olan Pending pattern'leri Approved yap + ApprovedAt set et.</summary>
    private async Task<ServiceResult<int>> PromoteApprovedPatternsAsync()
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            var count = await conn.ExecuteAsync(@"
                UPDATE AiPatterns SET
                    ApprovalStatus = 'Approved',
                    ApprovedAt = GETUTCDATE(),
                    UpdatedAt = GETUTCDATE()
                WHERE ApprovalStatus = 'Pending'
                  AND ConfidenceScore >= 8.0");
            return count;
        });
    }

    /// <summary>ConfidenceScore &lt; 0 olan pattern'leri Rejected yap.</summary>
    private async Task<ServiceResult<int>> CleanupRejectedPatternsAsync()
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            var count = await conn.ExecuteAsync(@"
                UPDATE AiPatterns SET
                    ApprovalStatus = 'Rejected',
                    DecayedAt = GETUTCDATE(),
                    UpdatedAt = GETUTCDATE()
                WHERE ApprovalStatus != 'Rejected'
                  AND ConfidenceScore < 0");
            return count;
        });
    }

    /// <summary>Belirli bir skill'in son 30 günlük performans özetini döner.</summary>
    public async Task<ServiceResult<SkillPerformanceSummary>> GetSkillPerformanceAsync(string skillId)
    {
        return await ExecuteServiceAsync<SkillPerformanceSummary>(async conn =>
        {
            var row = await conn.QueryFirstOrDefaultAsync<SkillPerformanceSummary>(@"
                SELECT
                    @SkillId AS SkillId,
                    COUNT(*) AS TotalInteractions,
                    SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessCount,
                    AVG(ConfidenceScore) AS AvgConfidence,
                    AVG(LatencyMs) AS AvgLatencyMs
                FROM AiInteractions
                WHERE SkillId = @SkillId
                  AND CreatedAt >= DATEADD(day, -30, GETUTCDATE())",
                new { SkillId = skillId });
            return row ?? new SkillPerformanceSummary { SkillId = skillId };
        });
    }
}

public class LearningCycleReport
{
    public int DecayedPatterns { get; set; }
    public int PromotedPatterns { get; set; }
    public int CleanedUpPatterns { get; set; }
}

public class SkillPerformanceSummary
{
    public string SkillId { get; set; } = string.Empty;
    public int TotalInteractions { get; set; }
    public int SuccessCount { get; set; }
    public decimal AvgConfidence { get; set; }
    public int AvgLatencyMs { get; set; }
    public decimal SuccessRate => TotalInteractions > 0
        ? (decimal)SuccessCount / TotalInteractions * 100
        : 0;
}
