namespace YonetIQ.Data.Models.AI;

/// <summary>
/// Proaktif AI içgörüsü. Dashboard briefing ve proaktif öneriler için kullanılır.
/// </summary>
public class AiInsight
{
    /// <summary>İçgörü tipi</summary>
    public required InsightType Type { get; set; }

    /// <summary>Öncelik seviyesi (UI sıralaması için)</summary>
    public InsightPriority Priority { get; set; } = InsightPriority.Normal;

    /// <summary>Başlık (kısa, ör: "3 görev gecikmiş")</summary>
    public required string Title { get; set; }

    /// <summary>Detay açıklaması</summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>İlişkili modül</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>İlişkili sayfa URL'si (navigasyon için)</summary>
    public string? NavigateUrl { get; set; }

    /// <summary>İkon (Bootstrap Icons)</summary>
    public string Icon { get; set; } = "bi-info-circle";

    /// <summary>İkon renk sınıfı</summary>
    public string IconColor { get; set; } = "text-muted";

    /// <summary>Veri kaynağı: deterministik mi AI üretimi mi?</summary>
    public bool IsAiGenerated { get; set; }

    /// <summary>Sayısal değer (varsa, ör: geciken görev sayısı)</summary>
    public int? NumericValue { get; set; }
}

public enum InsightType
{
    OverdueTasks,
    PendingDecisions,
    PendingApprovals,
    UpcomingMeetings,
    CompletedYesterday,
    FocusSuggestion,
    Anomaly,
    ChangeDetection,
    Risk,
    General
}

public enum InsightPriority
{
    Low,
    Normal,
    High,
    Critical
}
