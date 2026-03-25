using System.Security.Cryptography;
using System.Text;
using Dapper;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Öğrenme sinyallerini kaydeder, pattern güven skorunu hesaplar, decay uygular.
/// Sinyal ağırlıkları: ThumbsUp +3, ImplicitAccept +1, SqlAccepted +1.5,
/// NoRequery +0.5, ThumbsDown -5, SqlCorrection -2, Decay -0.3
/// Eşikler: ≥8 oto-onay, 4-7.9 admin onay, &lt;4 gürültü
/// </summary>
public class LearningSignalService(
    IConfiguration config, AuditService auditSvc,
    ILogger<LearningSignalService> logger)
    : BaseService(config, auditSvc)
{
    /// <summary>Yeni sinyal kaydeder ve pattern skorunu günceller.</summary>
    public async Task<ServiceResult> RecordSignalAsync(
        int patternId, string signalType, decimal weight,
        int? userId = null, int? sourceInteractionId = null, string? notes = null)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(@"
                INSERT INTO AiPatternSignals (PatternId, SignalType, Weight, UserId, SourceInteractionId, Notes, CreatedAt)
                VALUES (@PatternId, @SignalType, @Weight, @UserId, @SourceInteractionId, @Notes, GETUTCDATE())",
                new { PatternId = patternId, SignalType = signalType, Weight = weight,
                      UserId = userId, SourceInteractionId = sourceInteractionId, Notes = notes });

            // Pattern toplam skorunu yeniden hesapla
            await RecalculateScoreAsync(conn, patternId);

            logger.LogDebug("Signal recorded: Pattern={PatternId}, Type={Type}, Weight={Weight}",
                patternId, signalType, weight);
        });
    }

    /// <summary>SQL düzeltme sinyali — orijinal ve düzeltilmiş SQL karşılaştırır.</summary>
    public async Task<ServiceResult> RecordSqlCorrectionAsync(
        int patternId, string originalSql, string correctedSql,
        int userId, int? interactionId = null)
    {
        var sim = Similarity(originalSql, correctedSql);

        // Çok benzer (>0.95) → fark önemsiz, sinyal atla
        // Çok farklı (<0.10) → sıfırdan yazılmış, sinyal atla
        if (sim > 0.95 || sim < 0.10) return ServiceResult.Success();

        return await RecordSignalAsync(
            patternId, "SqlCorrection", -2.0m, userId, interactionId,
            $"Similarity: {sim:F2}");
    }

    /// <summary>30 günden eski kullanılmayan pattern'lere -0.3 decay uygular.</summary>
    public async Task<ServiceResult<int>> ApplyDecayAsync()
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            // 30 gündür kullanılmamış pattern'ler
            var stalePatterns = await conn.QueryAsync<int>(@"
                SELECT Id FROM AiPatterns
                WHERE (LastUsedAt IS NULL AND CreatedAt < DATEADD(day, -30, GETUTCDATE()))
                   OR (LastUsedAt IS NOT NULL AND LastUsedAt < DATEADD(day, -30, GETUTCDATE()))");

            var count = 0;
            foreach (var patternId in stalePatterns)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO AiPatternSignals (PatternId, SignalType, Weight, CreatedAt)
                    VALUES (@Id, 'Decay', -0.3, GETUTCDATE())",
                    new { Id = patternId });
                await RecalculateScoreAsync(conn, patternId);
                count++;
            }

            if (count > 0)
                logger.LogInformation("Decay applied to {Count} stale patterns", count);
            return count;
        });
    }

    private static async Task RecalculateScoreAsync(System.Data.IDbConnection conn, int patternId)
    {
        var totalScore = await conn.ExecuteScalarAsync<decimal?>(
            "SELECT SUM(Weight) FROM AiPatternSignals WHERE PatternId = @Id",
            new { Id = patternId }) ?? 0;

        var status = totalScore >= 8.0m ? "Approved"
                   : totalScore < 4.0m ? "Rejected"
                   : "Pending";

        await conn.ExecuteAsync(@"
            UPDATE AiPatterns SET
                ConfidenceScore = @Score,
                ApprovalStatus = @Status,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id",
            new { Score = totalScore, Status = status, Id = patternId });
    }

    /// <summary>İki string arasındaki benzerlik oranı (0-1). Levenshtein bazlı.</summary>
    internal static double Similarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        a = a.Trim().ToLowerInvariant();
        b = b.Trim().ToLowerInvariant();
        if (a == b) return 1.0;

        var maxLen = Math.Max(a.Length, b.Length);
        var dist = LevenshteinDistance(a, b);
        return 1.0 - ((double)dist / maxLen);
    }

    /// <summary>Case-insensitive SHA256 hash.</summary>
    internal static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes);
    }

    private static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];
        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;
        for (var i = 1; i <= n; i++)
        for (var j = 1; j <= m; j++)
        {
            var cost = s[i - 1] == t[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
        }
        return d[n, m];
    }
}
