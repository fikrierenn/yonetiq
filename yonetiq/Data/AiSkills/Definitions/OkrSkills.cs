using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.AiSkills.Definitions;

public static class OkrSkills
{
    public static SkillDefinition Progress => new()
    {
        Id = "okr.progress",
        Name = "OKR İlerleme Yorumu",
        Description = "OKR ilerleme verilerini analiz ederek durumu yorumlar ve risk tespiti yapar",
        Category = SkillCategory.Okr,
        TriggerMode = TriggerMode.Reactive,
        Module = "okr",
        Icon = "bi-graph-up-arrow",
        RequiredEntities = ["Objective"],
        RequiredContext = ["key_results", "progress_data"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.3f,
        SystemPromptFile = "okr.progress.system.md",
        UserPromptFile = "okr.progress.user.md",
        MaxOutputLength = 1500,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "İlerleme yorumu gerçek verilerle örtüşüyor",
        DependencyServices = ["OkrService"]
    };

    public static SkillDefinition Action => new()
    {
        Id = "okr.action",
        Name = "OKR Aksiyon Önerisi",
        Description = "Geride kalan OKR'ler için somut aksiyon adımları önerir",
        Category = SkillCategory.Okr,
        TriggerMode = TriggerMode.Reactive,
        Module = "okr",
        Icon = "bi-rocket-takeoff",
        RequiredEntities = ["Objective"],
        RequiredContext = ["key_results", "progress_data", "team_capacity"],
        OutputType = SkillOutputType.ActionList,
        Temperature = 0.4f,
        SystemPromptFile = "okr.action.system.md",
        UserPromptFile = "okr.action.user.md",
        MaxOutputLength = 2000,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Önerilen aksiyonlar uygulanabilir ve somut",
        DependencyServices = ["OkrService", "TaskService"]
    };
}
