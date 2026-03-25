using Dapper;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// AI skill tanımlarını DB'den (AiSkillDefinitions) yükler/kaydeder.
/// Startup'ta InitializeAsync ile registry'yi DB override'larıyla besler.
/// </summary>
public class SkillPersistenceService(
    IConfiguration config, AuditService auditSvc,
    SkillRegistry registry, ILogger<SkillPersistenceService> logger)
    : BaseService(config, auditSvc)
{
    /// <summary>
    /// DB'deki aktif skill tanımlarını yükleyip mevcut registry'deki skill'lerin
    /// Temperature, MaxTokenInput gibi alanlarını override eder.
    /// Startup'ta çağrılır.
    /// </summary>
    public async Task InitializeAsync()
    {
        var result = await ExecuteServiceAsync<List<AiSkillRecord>>(async conn =>
        {
            var rows = await conn.QueryAsync<AiSkillRecord>(@"
                SELECT Id, SkillId, Name, Description, Module, Category, TriggerMode,
                       Temperature, MaxTokenInput, MaxOutputLength,
                       SystemPromptOverride, UserPromptOverride,
                       IsEnabled, Version, UpdatedBy, UpdatedAt, CreatedAt
                FROM AiSkillDefinitions
                WHERE IsEnabled = 1");
            return rows.ToList();
        });

        if (!result.IsSuccess || result.Data is null) return;

        var count = 0;
        foreach (var record in result.Data)
        {
            var existing = registry.Resolve(record.SkillId);
            if (existing is null) continue;

            // DB'deki değerlerle override et
            existing.Temperature = (float)record.Temperature;
            existing.MaxTokenInput = record.MaxTokenInput;
            existing.MaxOutputLength = record.MaxOutputLength;

            count++;
        }

        logger.LogInformation("SkillPersistence: {Count} skill DB'den override edildi", count);
    }

    /// <summary>Skill kaydını DB'ye yazar (yeni veya güncelleme).</summary>
    public async Task<ServiceResult<int>> SaveAsync(AiSkillRecord record, int userId)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            var existing = await conn.ExecuteScalarAsync<int?>(
                "SELECT Id FROM AiSkillDefinitions WHERE SkillId = @SkillId",
                new { record.SkillId });

            if (existing.HasValue)
            {
                await conn.ExecuteAsync(@"
                    UPDATE AiSkillDefinitions SET
                        Name = @Name, Description = @Description, Module = @Module,
                        Category = @Category, TriggerMode = @TriggerMode,
                        Temperature = @Temperature, MaxTokenInput = @MaxTokenInput,
                        MaxOutputLength = @MaxOutputLength,
                        SystemPromptOverride = @SystemPromptOverride,
                        UserPromptOverride = @UserPromptOverride,
                        IsEnabled = @IsEnabled, Version = Version + 1,
                        UpdatedBy = @UserId, UpdatedAt = GETUTCDATE()
                    WHERE SkillId = @SkillId",
                    new
                    {
                        record.Name, record.Description, record.Module,
                        record.Category, record.TriggerMode,
                        record.Temperature, record.MaxTokenInput, record.MaxOutputLength,
                        record.SystemPromptOverride, record.UserPromptOverride,
                        record.IsEnabled, record.SkillId, UserId = userId
                    });
                LogAction("AiSkillDefinition", "Update", record.SkillId);
                return existing.Value;
            }

            var id = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO AiSkillDefinitions
                    (SkillId, Name, Description, Module, Category, TriggerMode,
                     Temperature, MaxTokenInput, MaxOutputLength,
                     SystemPromptOverride, UserPromptOverride, IsEnabled, Version, CreatedAt)
                VALUES
                    (@SkillId, @Name, @Description, @Module, @Category, @TriggerMode,
                     @Temperature, @MaxTokenInput, @MaxOutputLength,
                     @SystemPromptOverride, @UserPromptOverride, @IsEnabled, 1, GETUTCDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);", record);
            LogAction("AiSkillDefinition", "Create", record.SkillId);
            return id;
        });
    }

    /// <summary>DB'deki tüm skill kayıtlarını listeler.</summary>
    public async Task<ServiceResult<List<AiSkillRecord>>> ListAllAsync()
    {
        return await ExecuteServiceAsync<List<AiSkillRecord>>(async conn =>
        {
            var rows = await conn.QueryAsync<AiSkillRecord>(@"
                SELECT Id, SkillId, Name, Description, Module, Category, TriggerMode,
                       Temperature, MaxTokenInput, MaxOutputLength,
                       SystemPromptOverride, UserPromptOverride,
                       IsEnabled, Version, UpdatedBy, UpdatedAt, CreatedAt
                FROM AiSkillDefinitions
                ORDER BY Module, Name");
            return rows.ToList();
        });
    }

    /// <summary>Skill'i DB'de devre dışı bırakır.</summary>
    public async Task<ServiceResult> DisableAsync(string skillId, int userId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "UPDATE AiSkillDefinitions SET IsEnabled = 0, UpdatedBy = @UserId, UpdatedAt = GETUTCDATE() WHERE SkillId = @SkillId",
                new { SkillId = skillId, UserId = userId });
            registry.Disable(skillId);
            LogAction("AiSkillDefinition", "Disable", skillId);
        });
    }

    /// <summary>JSON array veya CSV formatındaki string'i array'e parse eder.</summary>
    internal static string[] ParseArr(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return [];
        input = input.Trim();
        if (input.StartsWith('['))
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<string[]>(input) ?? [];
            }
            catch { /* fallback to CSV */ }
        }
        return input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
