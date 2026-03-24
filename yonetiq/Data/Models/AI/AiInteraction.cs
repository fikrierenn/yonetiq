namespace YonetIQ.Data.Models.AI;

/// <summary>
/// Bir AI etkileşim kaydı. Her skill çağrısı bir AiInteraction üretir.
/// DB tablosu: AiInteractions
/// </summary>
public class AiInteraction
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Çalıştırılan skill ID'si (ör: "task.clarify")</summary>
    public string SkillId { get; set; } = string.Empty;

    /// <summary>Modül bağlamı (ör: "task", "meeting", "dashboard")</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>Kısa input özeti (tam input değil, gizlilik için)</summary>
    public string InputSummary { get; set; } = string.Empty;

    /// <summary>Kısa output özeti</summary>
    public string OutputSummary { get; set; } = string.Empty;

    /// <summary>AI güven skoru</summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>Yanıt süresi (ms)</summary>
    public int LatencyMs { get; set; }

    /// <summary>Kullanılan token sayısı (tahmini)</summary>
    public int TokenCount { get; set; }

    /// <summary>Başarılı mı?</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Oluşturulma zamanı (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
