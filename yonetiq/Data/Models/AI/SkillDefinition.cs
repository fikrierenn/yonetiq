namespace YonetIQ.Data.Models.AI;

/// <summary>
/// Bir AI skill'inin tüm meta verisini ve konfigürasyonunu tanımlar.
/// Skill, YonetIQ AI sistemindeki atomik, test edilebilir ve ölçülebilir bir AI yeteneğidir.
/// </summary>
public class SkillDefinition
{
    /// <summary>Benzersiz skill kimliği (ör: "task.clarify", "meeting.summarize")</summary>
    public required string Id { get; init; }

    /// <summary>İnsan okunabilir ad (ör: "Görev Netleştirme")</summary>
    public required string Name { get; init; }

    /// <summary>Kısa açıklama</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Skill kategorisi</summary>
    public required SkillCategory Category { get; init; }

    /// <summary>Tetiklenme modu</summary>
    public TriggerMode TriggerMode { get; init; } = TriggerMode.Reactive;

    /// <summary>Bu skill hangi modülde görünür? (ör: "task", "meeting", "dashboard", "global")</summary>
    public required string Module { get; init; }

    /// <summary>UI'da gösterilecek ikon (Bootstrap Icons sınıf adı)</summary>
    public string Icon { get; init; } = "bi-stars";

    // --- Input/Output ---

    /// <summary>Skill'in ihtiyaç duyduğu entity tipleri (ör: ["TaskItem", "User"])</summary>
    public string[] RequiredEntities { get; init; } = [];

    /// <summary>Skill'in ihtiyaç duyduğu bağlam verileri</summary>
    public string[] RequiredContext { get; init; } = [];

    /// <summary>Opsiyonel bağlam verileri</summary>
    public string[] OptionalContext { get; init; } = [];

    /// <summary>Kullanıcıdan metin girişi gerekli mi?</summary>
    public bool RequiresUserInput { get; init; }

    /// <summary>Çıktı tipi</summary>
    public SkillOutputType OutputType { get; init; } = SkillOutputType.Text;

    // --- AI Konfigürasyonu ---

    /// <summary>Gemini temperature (0.0 - 1.0)</summary>
    public float Temperature { get; set; } = 0.2f;

    /// <summary>System prompt dosya adı (Prompts klasöründe, ör: "task.clarify.system.md")</summary>
    public required string SystemPromptFile { get; init; }

    /// <summary>User prompt template dosya adı</summary>
    public required string UserPromptFile { get; init; }

    // --- Guardrails ---

    /// <summary>Maksimum input token sayısı (tahmini)</summary>
    public int MaxTokenInput { get; set; } = 4000;

    /// <summary>Maksimum çıktı uzunluğu (karakter)</summary>
    public int MaxOutputLength { get; set; } = 2000;

    /// <summary>Yasaklı eylemler</summary>
    public string[] ProhibitedActions { get; init; } = [];

    /// <summary>Minimum güven skoru eşiği (0.0 - 1.0)</summary>
    public float MinConfidence { get; init; } = 0.5f;

    /// <summary>AI başarısız olursa fallback davranışı açıklaması</summary>
    public string FallbackBehavior { get; init; } = "Yerel kurallarla basit özet üret";

    /// <summary>Hallucination risk seviyesi</summary>
    public RiskLevel HallucinationRisk { get; init; } = RiskLevel.Medium;

    // --- Evaluation ---

    /// <summary>Başarı kriteri açıklaması</summary>
    public string SuccessCriteria { get; init; } = string.Empty;

    /// <summary>Bağımlı servis adları</summary>
    public string[] DependencyServices { get; init; } = [];
}

public enum SkillCategory
{
    Executive,
    Task,
    Meeting,
    Note,
    Report,
    Query,
    Approval,
    Okr,
    Cross
}

public enum TriggerMode
{
    Reactive,
    Proactive,
    Hybrid
}

public enum SkillOutputType
{
    Text,
    StructuredData,
    ActionList,
    Suggestion
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}
