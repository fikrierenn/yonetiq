using Dapper;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// AI etkileşim geçmişi ve geri bildirim kayıtlarını yönetir.
/// Skill istatistiklerini hesaplar.
/// </summary>
public class AiMemoryService(IConfiguration config, AuditService auditService, ILogger<AiMemoryService> logger)
    : BaseService(config, auditService)
{
    /// <summary>
    /// Yeni bir AI etkileşimini veritabanına kaydeder ve oluşan ID'yi döner.
    /// </summary>
    public async Task<ServiceResult<int>> SaveInteractionAsync(AiInteraction interaction)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            const string sql = """
                INSERT INTO AiInteractions (UserId, SkillId, Module, InputSummary, OutputSummary,
                    ConfidenceScore, LatencyMs, TokenCount, IsSuccess, CreatedAt)
                VALUES (@UserId, @SkillId, @Module, @InputSummary, @OutputSummary,
                    @ConfidenceScore, @LatencyMs, @TokenCount, @IsSuccess, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

            var id = await conn.ExecuteScalarAsync<int>(sql, new
            {
                interaction.UserId,
                interaction.SkillId,
                interaction.Module,
                interaction.InputSummary,
                interaction.OutputSummary,
                interaction.ConfidenceScore,
                interaction.LatencyMs,
                interaction.TokenCount,
                interaction.IsSuccess,
                interaction.CreatedAt
            });

            LogAction("AI", "SaveInteraction", new { InteractionId = id, interaction.SkillId });
            logger.LogDebug("AI interaction saved: Id={Id}, Skill={SkillId}", id, interaction.SkillId);
            return id;
        });
    }

    /// <summary>
    /// Kullanıcının bir AI çıktısına verdiği geri bildirimi kaydeder.
    /// </summary>
    public async Task<ServiceResult> SaveFeedbackAsync(AiFeedback feedback)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            const string sql = """
                INSERT INTO AiFeedback (InteractionId, UserId, Rating, CorrectionText,
                    WasAccepted, WasEdited, CreatedAt)
                VALUES (@InteractionId, @UserId, @Rating, @CorrectionText,
                    @WasAccepted, @WasEdited, @CreatedAt);
                """;

            await conn.ExecuteAsync(sql, new
            {
                feedback.InteractionId,
                feedback.UserId,
                feedback.Rating,
                feedback.CorrectionText,
                feedback.WasAccepted,
                feedback.WasEdited,
                feedback.CreatedAt
            });

            LogAction("AI", "SaveFeedback", new { feedback.InteractionId, feedback.Rating });
        });
    }

    /// <summary>
    /// Kullanıcının son etkileşimlerini getirir. Opsiyonel olarak skill bazında filtreleme yapılabilir.
    /// </summary>
    public async Task<ServiceResult<List<AiInteraction>>> GetRecentInteractionsAsync(
        int userId, string? skillId = null, int limit = 10)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            var sql = """
                SELECT TOP(@Limit)
                    Id, UserId, SkillId, Module, InputSummary, OutputSummary,
                    ConfidenceScore, LatencyMs, TokenCount, IsSuccess, CreatedAt
                FROM AiInteractions
                WHERE UserId = @UserId
                """;

            if (!string.IsNullOrWhiteSpace(skillId))
                sql += " AND SkillId = @SkillId";

            sql += " ORDER BY CreatedAt DESC";

            var interactions = await conn.QueryAsync<AiInteraction>(sql, new
            {
                UserId = userId,
                SkillId = skillId,
                Limit = limit
            });

            return interactions.ToList();
        });
    }

    /// <summary>
    /// Belirli bir skill için istatistikleri hesaplar:
    /// toplam çağrı, ortalama gecikme, kabul oranı, ortalama puan.
    /// </summary>
    public async Task<ServiceResult<SkillStats>> GetSkillStatsAsync(string skillId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            const string sql = """
                SELECT
                    COUNT(*) AS TotalInvocations,
                    ISNULL(AVG(CAST(i.LatencyMs AS DECIMAL(18,2))), 0) AS AvgLatencyMs,
                    ISNULL(
                        CAST(SUM(CASE WHEN f.WasAccepted = 1 THEN 1 ELSE 0 END) AS DECIMAL(18,4))
                        / NULLIF(COUNT(f.Id), 0), 0
                    ) AS AcceptanceRate,
                    ISNULL(AVG(CAST(f.Rating AS DECIMAL(18,2))), 0) AS AvgRating
                FROM AiInteractions i
                LEFT JOIN AiFeedback f ON f.InteractionId = i.Id
                WHERE i.SkillId = @SkillId
                """;

            var stats = await conn.QuerySingleOrDefaultAsync<SkillStats>(sql, new { SkillId = skillId });
            return stats ?? new SkillStats(0, 0, 0, 0);
        });
    }

    // ── Golden Memory: Pattern Methods ──

    /// <summary>
    /// Belirli bir skill için onaylanmış kalıpları (pattern) getirir.
    /// En çok kullanılanlar önce gelir.
    /// </summary>
    public async Task<ServiceResult<List<AiPattern>>> GetApprovedPatternsAsync(string skillId, int limit = 3)
    {
        return await ExecuteServiceAsync<List<AiPattern>>(async conn =>
        {
            var result = await conn.QueryAsync<AiPattern>(@"
                SELECT TOP (@limit) Id, SkillId, PatternType, InputPattern, ApprovedOutput,
                       UsageCount, LastUsedAt, ApprovedBy, CreatedAt
                FROM AiPatterns
                WHERE SkillId = @skillId
                ORDER BY UsageCount DESC, CreatedAt DESC",
                new { skillId, limit });
            return result.ToList();
        });
    }

    /// <summary>
    /// Yeni kalıp kaydeder veya mevcut kalıbı günceller.
    /// </summary>
    public async Task<ServiceResult<int>> SavePatternAsync(AiPattern pattern)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            if (pattern.Id == 0)
            {
                return await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO AiPatterns (SkillId, PatternType, InputPattern, ApprovedOutput, ApprovedBy)
                    VALUES (@SkillId, @PatternType, @InputPattern, @ApprovedOutput, @ApprovedBy);
                    SELECT CAST(SCOPE_IDENTITY() AS INT)", pattern);
            }
            await conn.ExecuteAsync(@"
                UPDATE AiPatterns SET InputPattern=@InputPattern, ApprovedOutput=@ApprovedOutput,
                       PatternType=@PatternType, UpdatedAt=GETUTCDATE() WHERE Id=@Id", pattern);
            return pattern.Id;
        });
    }

    /// <summary>
    /// Kalıp kullanım sayacını artırır. Kritik olmayan — başarısız olursa ana akışı etkilemez.
    /// </summary>
    public async Task IncrementPatternUsageAsync(int patternId)
    {
        try
        {
            await using var conn = CreateConn();
            await conn.OpenAsync();
            await conn.ExecuteAsync(@"
                UPDATE AiPatterns SET UsageCount = UsageCount + 1, LastUsedAt = GETUTCDATE() WHERE Id = @patternId",
                new { patternId });
        }
        catch (Exception ex)
        {
            // Non-critical — don't fail the main flow
            System.Diagnostics.Debug.WriteLine($"Pattern usage increment failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Skill istatistik özeti.
/// </summary>
public record SkillStats(int TotalInvocations, decimal AvgLatencyMs, decimal AcceptanceRate, decimal AvgRating);
