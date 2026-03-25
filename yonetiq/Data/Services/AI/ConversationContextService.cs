using Dapper;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Çok turlu sohbet bağlamını yönetir (AiConversationContext tablosu).
/// Her kullanıcı+session için son MaxTurns tur saklanır.
/// ContextBuilder, sohbet geçmişini prompt'a enjekte ederken bu servisi kullanır.
/// </summary>
public class ConversationContextService(
    IConfiguration config, AuditService auditSvc,
    ILogger<ConversationContextService> logger)
    : BaseService(config, auditSvc)
{
    private const int MaxTurns = 6;

    /// <summary>Yeni bir sohbet turunu kaydeder.</summary>
    public async Task<ServiceResult> SaveTurnAsync(
        string sessionKey, int userId, string userInput,
        string? aiOutput, string? skillId = null)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            // Mevcut tur sayısını al
            var turnCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM AiConversationContext WHERE SessionKey = @Key",
                new { Key = sessionKey });

            await conn.ExecuteAsync(@"
                INSERT INTO AiConversationContext (SessionKey, UserId, TurnIndex, SkillId, UserInput, AiOutput, CreatedAt)
                VALUES (@SessionKey, @UserId, @TurnIndex, @SkillId, @UserInput, @AiOutput, GETUTCDATE())",
                new { SessionKey = sessionKey, UserId = userId,
                      TurnIndex = turnCount, SkillId = skillId,
                      UserInput = userInput, AiOutput = aiOutput });

            // MaxTurns aşıldıysa eski turları temizle
            if (turnCount >= MaxTurns)
            {
                await conn.ExecuteAsync(@"
                    DELETE FROM AiConversationContext
                    WHERE SessionKey = @Key
                      AND TurnIndex < (
                          SELECT MIN(TurnIndex) FROM (
                              SELECT TOP (@Keep) TurnIndex
                              FROM AiConversationContext
                              WHERE SessionKey = @Key
                              ORDER BY TurnIndex DESC
                          ) sub
                      )",
                    new { Key = sessionKey, Keep = MaxTurns });
            }

            logger.LogDebug("ConversationTurn saved: Session={Session}, Turn={Turn}",
                sessionKey, turnCount);
        });
    }

    /// <summary>Session'daki son turları getirir (en eski → en yeni sıralı).</summary>
    public async Task<ServiceResult<List<AiConversationTurn>>> GetRecentTurnsAsync(
        string sessionKey, int? maxTurns = null)
    {
        return await ExecuteServiceAsync<List<AiConversationTurn>>(async conn =>
        {
            var limit = maxTurns ?? MaxTurns;
            var turns = await conn.QueryAsync<AiConversationTurn>(@"
                SELECT TOP (@Limit) Id, SessionKey, UserId, TurnIndex, SkillId, UserInput, AiOutput, CreatedAt
                FROM AiConversationContext
                WHERE SessionKey = @Key
                ORDER BY TurnIndex DESC",
                new { Key = sessionKey, Limit = limit });
            return turns.Reverse().ToList(); // en eski önce
        });
    }

    /// <summary>Session'daki tüm turları siler.</summary>
    public async Task<ServiceResult> ClearSessionAsync(string sessionKey)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "DELETE FROM AiConversationContext WHERE SessionKey = @Key",
                new { Key = sessionKey });
        });
    }

    /// <summary>Sohbet geçmişini prompt'a enjekte edilecek metin formatında döner.</summary>
    public async Task<string> BuildContextSummaryAsync(string sessionKey)
    {
        var result = await GetRecentTurnsAsync(sessionKey);
        if (!result.IsSuccess || result.Data is null || result.Data.Count == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Önceki Sohbet:");
        foreach (var turn in result.Data)
        {
            sb.AppendLine($"  Kullanıcı: {turn.UserInput}");
            if (!string.IsNullOrEmpty(turn.AiOutput))
                sb.AppendLine($"  AI: {Truncate(turn.AiOutput, 200)}");
        }
        return sb.ToString();
    }

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..maxLen] + "…";
}
