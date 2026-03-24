using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.AiSkills.Definitions;

public static class TaskSkills
{
    public static SkillDefinition Clarify => new()
    {
        Id = "task.clarify",
        Name = "Görev Netleştirme",
        Description = "Belirsiz görev tanımını netleştirir, teslim kriteri ve adımlar önerir",
        Category = SkillCategory.Task,
        TriggerMode = TriggerMode.Reactive,
        Module = "task",
        Icon = "bi-lightbulb",
        RequiredEntities = ["TaskItem"],
        RequiredContext = ["assignee_info"],
        OptionalContext = ["related_tasks"],
        OutputType = SkillOutputType.Suggestion,
        Temperature = 0.3f,
        SystemPromptFile = "task.clarify.system.md",
        UserPromptFile = "task.clarify.user.md",
        MaxOutputLength = 1500,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Kullanıcı önerilen netleştirmeyi kabul etti veya düzenleyerek kullandı",
        DependencyServices = ["TaskService"]
    };

    public static SkillDefinition Risk => new()
    {
        Id = "task.risk",
        Name = "Risk Analizi",
        Description = "Görevdeki riskleri, tıkanıklıkları ve potansiyel gecikmeleri tespit eder",
        Category = SkillCategory.Task,
        TriggerMode = TriggerMode.Hybrid,
        Module = "task",
        Icon = "bi-exclamation-triangle",
        RequiredEntities = ["TaskItem"],
        RequiredContext = ["assignee_workload", "overdue_status"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "task.risk.system.md",
        UserPromptFile = "task.risk.user.md",
        MaxOutputLength = 1000,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Tespit edilen risk gerçek verilerle doğrulanabilir",
        DependencyServices = ["TaskService", "UserService"]
    };

    public static SkillDefinition Decompose => new()
    {
        Id = "task.decompose",
        Name = "Alt Görev Önerisi",
        Description = "Büyük görevi yönetilebilir alt görevlere parçalar",
        Category = SkillCategory.Task,
        TriggerMode = TriggerMode.Reactive,
        Module = "task",
        Icon = "bi-diagram-3",
        RequiredEntities = ["TaskItem"],
        RequiredContext = ["assignee_info"],
        OptionalContext = ["related_tasks"],
        OutputType = SkillOutputType.ActionList,
        Temperature = 0.3f,
        SystemPromptFile = "task.decompose.system.md",
        UserPromptFile = "task.decompose.user.md",
        MaxOutputLength = 1500,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Önerilen alt görevler kullanıcı tarafından kabul edildi",
        DependencyServices = ["TaskService"]
    };

    public static SkillDefinition Bottleneck => new()
    {
        Id = "task.bottleneck",
        Name = "Tıkanıklık Analizi",
        Description = "Ekip genelinde tıkanıklık gösteren görevleri ve darboğazları saptar",
        Category = SkillCategory.Task,
        TriggerMode = TriggerMode.Proactive,
        Module = "task",
        Icon = "bi-sign-stop",
        RequiredEntities = ["TaskItem"],
        RequiredContext = ["assignee_workload", "overdue_status"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.2f,
        SystemPromptFile = "task.bottleneck.system.md",
        UserPromptFile = "task.bottleneck.user.md",
        MaxOutputLength = 1200,
        HallucinationRisk = RiskLevel.Medium,
        SuccessCriteria = "Tespit edilen tıkanıklık gerçek verilerle doğrulanabilir",
        DependencyServices = ["TaskService", "UserService"]
    };

    public static SkillDefinition Followup => new()
    {
        Id = "task.followup",
        Name = "Takip Mesajı",
        Description = "Geciken görev için profesyonel takip mesajı taslağı oluşturur",
        Category = SkillCategory.Task,
        TriggerMode = TriggerMode.Reactive,
        Module = "task",
        Icon = "bi-envelope-arrow-up",
        RequiredEntities = ["TaskItem"],
        RequiredContext = ["assignee_info", "overdue_status"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.5f,
        SystemPromptFile = "task.followup.system.md",
        UserPromptFile = "task.followup.user.md",
        MaxOutputLength = 500,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Kullanıcı mesajı doğrudan gönderdi veya düzenleyerek kullandı",
        DependencyServices = ["TaskService"]
    };

    public static SkillDefinition Prioritize => new()
    {
        Id = "task.prioritize",
        Name = "Önceliklendirme",
        Description = "Görev listesini etki ve aciliyet kriterlerine göre önceliklendirir",
        Category = SkillCategory.Task,
        TriggerMode = TriggerMode.Reactive,
        Module = "task",
        Icon = "bi-sort-numeric-down",
        RequiredEntities = ["TaskItem"],
        RequiredContext = ["overdue_status"],
        OutputType = SkillOutputType.StructuredData,
        Temperature = 0.2f,
        SystemPromptFile = "task.prioritize.system.md",
        UserPromptFile = "task.prioritize.user.md",
        MaxOutputLength = 1500,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Kullanıcı önerilen sıralamayı uyguladı",
        DependencyServices = ["TaskService"]
    };
}
