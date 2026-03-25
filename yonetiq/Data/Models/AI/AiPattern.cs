namespace YonetIQ.Data.Models.AI;

/// <summary>
/// Onaylanmış AI girdi/çıktı kalıplarını saklar.
/// Golden memory sistemi: başarılı çıktılar few-shot örnek olarak yeniden kullanılır.
/// </summary>
public class AiPattern
{
    public int Id { get; set; }
    public string SkillId { get; set; } = string.Empty;

    /// <summary>Kalıp tipi: "good_output", "correction", "preference"</summary>
    public string PatternType { get; set; } = string.Empty;

    public string InputPattern { get; set; } = string.Empty;
    public string ApprovedOutput { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // WP1.3 — Öğrenme sistemi genişletmeleri
    public decimal ConfidenceScore { get; set; }

    /// <summary>Pending, Approved, Rejected</summary>
    public string ApprovalStatus { get; set; } = "Pending";

    public DateTime? DecayedAt { get; set; }
    public bool IsCorrectionDerived { get; set; }
    public int? ProposedByUserId { get; set; }
    public int? SourceInteractionId { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
