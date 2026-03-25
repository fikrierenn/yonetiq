using System.Text.Json;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Meta-skill'leri yönetir: skill analizi, AI ile skill üretme, prompt iyileştirme.
/// SkillStudio sayfasından çağrılır.
/// </summary>
public class MetaSkillService(
    AiOrchestrationService orchestration,
    SkillRegistry registry,
    SkillPersistenceService persistence,
    ContinuousLearningService learningSvc,
    PromptEngine promptEngine,
    ILogger<MetaSkillService> logger)
{
    /// <summary>Skill fikrini analiz eder — uygulanabilirlik, risk, yaklaşım önerisi.</summary>
    public async Task<AiResponse> AnalyzeSkillIdeaAsync(string skillIdea, int userId)
    {
        var existingSkills = string.Join("\n",
            registry.ListAll().Select(s => $"- {s.Id}: {s.Name} ({s.Module})"));

        var request = new AiRequest
        {
            SkillId = "meta.analyzer",
            UserId = userId,
            Module = "admin",
            UserInput = skillIdea,
            Parameters = new()
            {
                ["skill_idea"] = skillIdea,
                ["existing_skills"] = existingSkills,
                ["available_services"] = "TaskService, MeetingService, NoteService, QueryService, OkrService, ApprovalService, UserService"
            }
        };

        return await orchestration.ProcessRequestAsync(request);
    }

    /// <summary>AI ile yeni skill tanımı + prompt çifti üretir.</summary>
    public async Task<ServiceResult<SkillWriterOutput>> GenerateSkillAsync(string skillSpec, int userId)
    {
        var existingSkills = string.Join("\n",
            registry.ListAll().Select(s => $"- {s.Id}: {s.Name}"));

        var request = new AiRequest
        {
            SkillId = "meta.writer",
            UserId = userId,
            Module = "admin",
            UserInput = skillSpec,
            Parameters = new()
            {
                ["skill_spec"] = skillSpec,
                ["existing_skills"] = existingSkills
            }
        };

        var response = await orchestration.ProcessRequestAsync(request);
        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            return ServiceResult<SkillWriterOutput>.Failure(
                response.ErrorMessage ?? "Skill üretimi başarısız");

        // JSON parse
        try
        {
            var jsonContent = ExtractJson(response.Content);
            var output = JsonSerializer.Deserialize<SkillWriterOutput>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (output is null || string.IsNullOrEmpty(output.SkillId))
                return ServiceResult<SkillWriterOutput>.Failure("Geçersiz skill JSON: skillId eksik");

            return ServiceResult<SkillWriterOutput>.Success(output);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "MetaSkill JSON parse hatası");
            return ServiceResult<SkillWriterOutput>.Failure($"AI çıktısı geçerli JSON değil: {ex.Message}");
        }
    }

    /// <summary>Üretilen skill'i DB'ye kaydeder ve prompt dosyalarını diske yazar.</summary>
    public async Task<ServiceResult<string>> PublishSkillAsync(SkillWriterOutput output, int userId)
    {
        if (string.IsNullOrEmpty(output.SkillId))
            return ServiceResult<string>.Failure("SkillId boş olamaz");

        // Zaten var mı?
        if (registry.Resolve(output.SkillId) is not null)
            return ServiceResult<string>.Failure($"'{output.SkillId}' zaten kayıtlı");

        // Prompt dosyalarını yaz
        var promptDir = Path.Combine(AppContext.BaseDirectory, "Data", "AiSkills", "Prompts");
        if (!Directory.Exists(promptDir)) Directory.CreateDirectory(promptDir);

        var systemFile = $"{output.SkillId}.system.md";
        var userFile = $"{output.SkillId}.user.md";

        await File.WriteAllTextAsync(
            Path.Combine(promptDir, systemFile),
            $"---version: 1---\n{output.SystemPrompt}");
        await File.WriteAllTextAsync(
            Path.Combine(promptDir, userFile),
            $"---version: 1---\n{output.UserPrompt}");

        // DB kaydı
        var record = new AiSkillRecord
        {
            SkillId = output.SkillId,
            Name = output.Name ?? output.SkillId,
            Description = output.Description,
            Module = output.Module ?? "global",
            Category = output.Category ?? "Query",
            TriggerMode = output.TriggerMode ?? "Reactive",
            Temperature = output.Temperature > 0 ? output.Temperature : 0.2m,
            MaxOutputLength = 2000,
            IsEnabled = true
        };
        await persistence.SaveAsync(record, userId);

        // Runtime registry'ye ekle
        var skill = new SkillDefinition
        {
            Id = output.SkillId,
            Name = record.Name,
            Description = record.Description ?? "",
            Category = Enum.TryParse<SkillCategory>(record.Category, true, out var cat) ? cat : SkillCategory.Query,
            TriggerMode = record.TriggerMode == "Proactive" ? TriggerMode.Proactive : TriggerMode.Reactive,
            Module = record.Module,
            Icon = "bi-stars",
            SystemPromptFile = systemFile,
            UserPromptFile = userFile,
            Temperature = (float)record.Temperature,
            MaxOutputLength = record.MaxOutputLength
        };
        registry.Register(skill);

        logger.LogInformation("Meta-skill published: {SkillId}", output.SkillId);
        return ServiceResult<string>.Success(output.SkillId, $"Skill '{output.Name}' başarıyla yayınlandı");
    }

    /// <summary>Mevcut skill'in prompt'unu iyileştirir.</summary>
    public async Task<AiResponse> RefinePromptAsync(string skillId, int userId)
    {
        var skill = registry.Resolve(skillId);
        if (skill is null)
            return AiResponse.Failure($"Skill bulunamadı: {skillId}", skillId);

        var currentPrompt = await promptEngine.LoadPromptAsync(skill.SystemPromptFile);
        var perfResult = await learningSvc.GetSkillPerformanceAsync(skillId);
        var perfSummary = perfResult.IsSuccess && perfResult.Data is not null
            ? $"Toplam: {perfResult.Data.TotalInteractions}, Başarı: %{perfResult.Data.SuccessRate:F0}, Ortalama güven: {perfResult.Data.AvgConfidence:F2}"
            : "Yeterli veri yok";

        var request = new AiRequest
        {
            SkillId = "meta.refiner",
            UserId = userId,
            Module = "admin",
            UserInput = $"Skill: {skillId}",
            Parameters = new()
            {
                ["skill_id"] = skillId,
                ["current_prompt"] = currentPrompt,
                ["feedback_summary"] = "Henüz detaylı feedback özeti yok",
                ["performance_stats"] = perfSummary
            }
        };

        return await orchestration.ProcessRequestAsync(request);
    }

    /// <summary>Düşük performanslı skill'leri analiz eder.</summary>
    public async Task<List<string>> AnalyzeLowPerformingSkillsAsync(int adminUserId)
    {
        var results = new List<string>();
        foreach (var skill in registry.ListAll())
        {
            var perf = await learningSvc.GetSkillPerformanceAsync(skill.Id);
            if (perf.IsSuccess && perf.Data is { TotalInteractions: > 10, SuccessRate: < 50 })
            {
                results.Add($"{skill.Id}: %{perf.Data.SuccessRate:F0} başarı ({perf.Data.TotalInteractions} çağrı)");
            }
        }
        return results;
    }

    private static string ExtractJson(string content)
    {
        // ```json ... ``` bloğu varsa çıkar
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start >= 0 && end > start)
            return content[start..(end + 1)];
        return content;
    }
}

/// <summary>AI'ın ürettiği skill tanımı JSON çıktısı.</summary>
public class SkillWriterOutput
{
    public string SkillId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? TriggerMode { get; set; }
    public string? Module { get; set; }
    public string? OutputType { get; set; }
    public decimal Temperature { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
}
