using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.AiSkills.Definitions;

public static class MeetingSkills
{
    public static SkillDefinition Summarize => new()
    {
        Id = "meeting.summarize",
        Name = "Toplantı Özeti",
        Description = "Toplantı bilgilerinden yapılandırılmış özet, kararlar ve aksiyon maddeleri üretir",
        Category = SkillCategory.Meeting,
        TriggerMode = TriggerMode.Reactive,
        Module = "meeting",
        Icon = "bi-card-text",
        RequiredEntities = ["Meeting"],
        RequiredContext = ["decisions", "participants"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.3f,
        SystemPromptFile = "meeting.summarize.system.md",
        UserPromptFile = "meeting.summarize.user.md",
        MaxOutputLength = 2000,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Toplantı özeti katılımcılar tarafından doğrulandı",
        DependencyServices = ["MeetingService"]
    };

    public static SkillDefinition ExtractActions => new()
    {
        Id = "meeting.extract_actions",
        Name = "Aksiyon Çıkarımı",
        Description = "Toplantı notlarından somut, atanabilir görev adayları çıkarır",
        Category = SkillCategory.Meeting,
        TriggerMode = TriggerMode.Reactive,
        Module = "meeting",
        Icon = "bi-list-task",
        RequiredEntities = ["Meeting"],
        RequiredContext = ["decisions", "participants"],
        OutputType = SkillOutputType.ActionList,
        Temperature = 0.2f,
        SystemPromptFile = "meeting.extract_actions.system.md",
        UserPromptFile = "meeting.extract_actions.user.md",
        MaxOutputLength = 2000,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Çıkarılan aksiyonlar göreve dönüştürüldü",
        DependencyServices = ["MeetingService", "TaskService"]
    };

    public static SkillDefinition ExtractDecisions => new()
    {
        Id = "meeting.extract_decisions",
        Name = "Karar Çıkarımı",
        Description = "Toplantı notlarından alınan kararları yapılandırılmış şekilde ayrıştırır",
        Category = SkillCategory.Meeting,
        TriggerMode = TriggerMode.Reactive,
        Module = "meeting",
        Icon = "bi-check2-square",
        RequiredEntities = ["Meeting"],
        RequiredContext = ["decisions", "participants"],
        OutputType = SkillOutputType.ActionList,
        Temperature = 0.2f,
        SystemPromptFile = "meeting.extract_decisions.system.md",
        UserPromptFile = "meeting.extract_decisions.user.md",
        MaxOutputLength = 1500,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Çıkarılan kararlar toplantı katılımcıları tarafından doğrulandı",
        DependencyServices = ["MeetingService"]
    };

    public static SkillDefinition Preparation => new()
    {
        Id = "meeting.preparation",
        Name = "Hazırlık Önerisi",
        Description = "Yaklaşan toplantı için gündem önerisi ve hazırlık kontrol listesi oluşturur",
        Category = SkillCategory.Meeting,
        TriggerMode = TriggerMode.Proactive,
        Module = "meeting",
        Icon = "bi-clipboard-check",
        RequiredEntities = ["Meeting"],
        RequiredContext = ["decisions", "participants"],
        OptionalContext = ["pending_tasks"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.4f,
        SystemPromptFile = "meeting.preparation.system.md",
        UserPromptFile = "meeting.preparation.user.md",
        MaxOutputLength = 1200,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Hazırlık önerisi toplantı öncesinde faydalı bulundu",
        DependencyServices = ["MeetingService", "TaskService"]
    };

    public static SkillDefinition Unresolved => new()
    {
        Id = "meeting.unresolved",
        Name = "Çözümsüz Tespit",
        Description = "Önceki toplantılardan kalan çözümsüz konuları ve takipsiz aksiyonları tespit eder",
        Category = SkillCategory.Meeting,
        TriggerMode = TriggerMode.Reactive,
        Module = "meeting",
        Icon = "bi-question-circle",
        RequiredEntities = ["Meeting"],
        RequiredContext = ["decisions", "participants"],
        OptionalContext = ["pending_tasks"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "meeting.unresolved.system.md",
        UserPromptFile = "meeting.unresolved.user.md",
        MaxOutputLength = 1200,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Tespit edilen çözümsüz konular doğrulandı",
        DependencyServices = ["MeetingService", "TaskService"]
    };
}
