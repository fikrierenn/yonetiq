using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.AiSkills.Definitions;

public static class NoteSkills
{
    public static SkillDefinition Structure => new()
    {
        Id = "note.structure",
        Name = "Not Yapılandırma",
        Description = "Dağınık notu başlıklı, maddeli ve yapılandırılmış formata dönüştürür",
        Category = SkillCategory.Note,
        TriggerMode = TriggerMode.Reactive,
        Module = "note",
        Icon = "bi-journal-text",
        RequiredEntities = ["PersonalNote"],
        RequiredContext = ["user_tags"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.3f,
        SystemPromptFile = "note.structure.system.md",
        UserPromptFile = "note.structure.user.md",
        MaxOutputLength = 2000,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Kullanıcı yapılandırılmış notu kabul etti",
        DependencyServices = ["NoteService"]
    };

    public static SkillDefinition SuggestTags => new()
    {
        Id = "note.suggest_tags",
        Name = "Etiket Önerisi",
        Description = "Not içeriğini analiz ederek uygun etiketler önerir",
        Category = SkillCategory.Note,
        TriggerMode = TriggerMode.Reactive,
        Module = "note",
        Icon = "bi-tags",
        RequiredEntities = ["PersonalNote"],
        RequiredContext = ["existing_tags"],
        OutputType = SkillOutputType.Suggestion,
        Temperature = 0.3f,
        SystemPromptFile = "note.suggest_tags.system.md",
        UserPromptFile = "note.suggest_tags.user.md",
        MaxOutputLength = 500,
        HallucinationRisk = RiskLevel.Low,
        DependencyServices = ["NoteService"]
    };

    public static SkillDefinition ExtractActions => new()
    {
        Id = "note.extract_actions",
        Name = "Aksiyon Çıkarımı",
        Description = "Kişisel notlardan somut, atanabilir görev adayları çıkarır",
        Category = SkillCategory.Note,
        TriggerMode = TriggerMode.Reactive,
        Module = "note",
        Icon = "bi-list-task",
        RequiredEntities = ["PersonalNote"],
        OutputType = SkillOutputType.ActionList,
        Temperature = 0.2f,
        SystemPromptFile = "note.extract_actions.system.md",
        UserPromptFile = "note.extract_actions.user.md",
        MaxOutputLength = 1200,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Çıkarılan aksiyonlar göreve dönüştürüldü",
        DependencyServices = ["NoteService", "TaskService"]
    };

    public static SkillDefinition ToAgenda => new()
    {
        Id = "note.to_agenda",
        Name = "Gündem Dönüşümü",
        Description = "Kişisel notu profesyonel toplantı gündemi formatına dönüştürür",
        Category = SkillCategory.Note,
        TriggerMode = TriggerMode.Reactive,
        Module = "note",
        Icon = "bi-calendar-plus",
        RequiredEntities = ["PersonalNote"],
        OutputType = SkillOutputType.Text,
        Temperature = 0.4f,
        SystemPromptFile = "note.to_agenda.system.md",
        UserPromptFile = "note.to_agenda.user.md",
        MaxOutputLength = 1000,
        HallucinationRisk = RiskLevel.Low,
        SuccessCriteria = "Gündem formatı toplantı oluşturmada kullanıldı",
        DependencyServices = ["NoteService"]
    };
}
