namespace YonetIQ.Data.Models.AI;

/// <summary>
/// AI orchestration'dan dönen yanıt modeli.
/// </summary>
public class AiResponse
{
    /// <summary>İşlem başarılı mı?</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Kullanılan skill ID'si</summary>
    public string SkillId { get; set; } = string.Empty;

    /// <summary>Skill adı (UI'da gösterilir)</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>AI çıktısı (Markdown formatında)</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Güven skoru (0.0 - 1.0)</summary>
    public float Confidence { get; set; }

    /// <summary>Güven seviyesi etiketi</summary>
    public string ConfidenceLabel => Confidence switch
    {
        > 0.8f => "Yüksek",
        > 0.5f => "Orta",
        _ => "Düşük"
    };

    /// <summary>Çıktı tipi</summary>
    public SkillOutputType OutputType { get; set; } = SkillOutputType.Text;

    /// <summary>Hata mesajı (başarısız ise)</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gecikme süresi (ms)</summary>
    public int LatencyMs { get; set; }

    /// <summary>Etkileşim ID'si (feedback için)</summary>
    public int? InteractionId { get; set; }

    /// <summary>Önerilen aksiyonlar (varsa)</summary>
    public List<SuggestedAction> Actions { get; set; } = [];

    /// <summary>Bağlam açıklaması — "AI bunu neden söyledi?"</summary>
    public string? ContextExplanation { get; set; }

    public static AiResponse Success(string content, string skillId, string skillName, float confidence = 0.7f) => new()
    {
        IsSuccess = true,
        Content = content,
        SkillId = skillId,
        SkillName = skillName,
        Confidence = confidence
    };

    public static AiResponse Failure(string error, string skillId = "") => new()
    {
        IsSuccess = false,
        ErrorMessage = error,
        SkillId = skillId
    };
}

/// <summary>
/// AI çıktısıyla birlikte sunulan aksiyon önerisi.
/// </summary>
public class SuggestedAction
{
    /// <summary>Aksiyon etiketi (ör: "Göreve Dönüştür", "Kopyala")</summary>
    public required string Label { get; set; }

    /// <summary>Aksiyon tipi</summary>
    public required ActionType Type { get; set; }

    /// <summary>Aksiyon parametreleri</summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>Bootstrap Icons sınıf adı</summary>
    public string Icon { get; set; } = "bi-arrow-right";
}

public enum ActionType
{
    ConvertToTask,
    CopyToClipboard,
    NavigateTo,
    CreateDecision,
    SendMessage,
    Custom
}
