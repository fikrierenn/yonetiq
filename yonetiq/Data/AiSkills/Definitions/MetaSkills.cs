using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.AiSkills.Definitions;

/// <summary>
/// Meta-skill'ler: AI'ın kendi skill'lerini analiz etmesi, yeni skill yazması ve prompt iyileştirmesi.
/// SkillStudio sayfasından tetiklenir.
/// </summary>
public static class MetaSkills
{
    /// <summary>Bir skill fikrini analiz eder, uygulanabilirliğini ve yaklaşımı değerlendirir.</summary>
    public static SkillDefinition SkillAnalyzer => new()
    {
        Id = "meta.analyzer",
        Name = "Skill Analizi",
        Description = "Yeni bir skill fikrinin uygulanabilirliğini, mevcut veri yapılarıyla uyumunu ve potansiyel risklerini analiz eder",
        Category = SkillCategory.Executive,
        TriggerMode = TriggerMode.Reactive,
        Module = "admin",
        Icon = "bi-search",
        RequiredEntities = [],
        RequiredContext = ["skill_idea", "existing_skills", "available_services"],
        OutputType = SkillOutputType.StructuredData,
        Temperature = 0.3f,
        SystemPromptFile = "meta.analyzer.system.md",
        UserPromptFile = "meta.analyzer.user.md",
        MaxOutputLength = 2000,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Analiz yapısal formatta, somut öneri ve risk değerlendirmesi içeriyor",
        DependencyServices = ["SkillRegistry", "SkillPersistenceService"]
    };

    /// <summary>Skill tanımı + prompt'ları AI ile üretir.</summary>
    public static SkillDefinition SkillWriter => new()
    {
        Id = "meta.writer",
        Name = "Skill Üretici",
        Description = "Verilen açıklamaya göre yeni bir skill tanımı (JSON) ve system/user prompt çifti üretir",
        Category = SkillCategory.Executive,
        TriggerMode = TriggerMode.Reactive,
        Module = "admin",
        Icon = "bi-pencil-square",
        RequiredEntities = [],
        RequiredContext = ["skill_spec", "existing_skills"],
        OutputType = SkillOutputType.StructuredData,
        Temperature = 0.4f,
        SystemPromptFile = "meta.writer.system.md",
        UserPromptFile = "meta.writer.user.md",
        MaxOutputLength = 4000,
        HallucinationRisk = RiskLevel.High,
        SuccessCriteria = "Üretilen JSON geçerli, prompt'lar tutarlı ve çalıştırılabilir",
        DependencyServices = ["SkillRegistry", "SkillPersistenceService"]
    };

    /// <summary>Mevcut bir prompt'u performans verilerine göre iyileştirir.</summary>
    public static SkillDefinition PromptRefiner => new()
    {
        Id = "meta.refiner",
        Name = "Prompt İyileştirici",
        Description = "Mevcut bir skill'in prompt'unu kullanıcı geri bildirimleri ve performans verilerine göre iyileştirir",
        Category = SkillCategory.Executive,
        TriggerMode = TriggerMode.Reactive,
        Module = "admin",
        Icon = "bi-magic",
        RequiredEntities = [],
        RequiredContext = ["current_prompt", "feedback_summary", "performance_stats"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.3f,
        SystemPromptFile = "meta.refiner.system.md",
        UserPromptFile = "meta.refiner.user.md",
        MaxOutputLength = 3000,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "İyileştirilmiş prompt daha yüksek kullanıcı memnuniyeti sağlıyor",
        DependencyServices = ["SkillRegistry", "AiMemoryService", "ContinuousLearningService"]
    };
}
