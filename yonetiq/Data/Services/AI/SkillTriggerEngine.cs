using Dapper;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Proaktif ve reaktif skill tetikleme kararlarını yönetir.
/// AiSkillTriggerLog tablosuna denetim kaydı yazar.
/// </summary>
public class SkillTriggerEngine(
    IConfiguration config, AuditService auditSvc,
    SkillRegistry registry)
    : BaseService(config, auditSvc)
{
    /// <summary>
    /// Verilen modül ve bağlam için tetiklenmesi gereken proaktif skill'leri döner.
    /// </summary>
    public List<SkillDefinition> GetProactiveSkills(string module)
    {
        return registry.ResolveByModule(module, TriggerMode.Proactive);
    }

    /// <summary>
    /// Kullanıcı girdisine en uygun reaktif skill'i çözer.
    /// Basit keyword eşleştirmesi — ileride ML ile değiştirilebilir.
    /// </summary>
    public SkillDefinition? ResolveReactiveSkill(string module, string userInput)
    {
        var candidates = registry.ResolveByModule(module, TriggerMode.Reactive);
        if (candidates.Count == 0) return null;

        // Basit skor: skill adı veya açıklaması user input ile ne kadar örtüşüyor
        var inputLower = userInput.ToLowerInvariant();
        SkillDefinition? best = null;
        var bestScore = 0;

        foreach (var skill in candidates)
        {
            var score = 0;
            var keywords = skill.Name.ToLowerInvariant().Split(' ');
            foreach (var kw in keywords)
            {
                if (kw.Length > 2 && inputLower.Contains(kw))
                    score += kw.Length;
            }
            if (score > bestScore) { best = skill; bestScore = score; }
        }

        return best;
    }

    /// <summary>Skill tetikleme denetim kaydı yazar.</summary>
    public async Task LogTriggerAsync(
        string skillId, string triggerSource, int userId,
        string? module = null, bool wasExecuted = true,
        int durationMs = 0, bool isSuccess = true)
    {
        await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(@"
                INSERT INTO AiSkillTriggerLog
                    (SkillId, TriggerSource, UserId, Module, WasExecuted, DurationMs, IsSuccess, CreatedAt)
                VALUES
                    (@SkillId, @TriggerSource, @UserId, @Module, @WasExecuted, @DurationMs, @IsSuccess, GETUTCDATE())",
                new { SkillId = skillId, TriggerSource = triggerSource,
                      UserId = userId, Module = module,
                      WasExecuted = wasExecuted, DurationMs = durationMs, IsSuccess = isSuccess });
        });
    }
}
