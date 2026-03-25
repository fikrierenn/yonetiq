namespace YonetIQ.Data.Models.AI;

/// <summary>
/// Semantik terim keşif adayı — kullanıcı sorgularından tespit edilen yeni iş terimleri.
/// SemanticDiscoveryService aday önerir, admin onaylar/reddeder.
/// </summary>
public class SemanticLearningCandidate
{
    public int Id { get; set; }
    public string Term { get; set; } = string.Empty;
    public string? DetectedInSkillId { get; set; }
    public string? DetectedInInput { get; set; }
    public int Frequency { get; set; } = 1;
    public string? ProposedDefinition { get; set; }

    /// <summary>Pending, Approved, Rejected</summary>
    public string Status { get; set; } = "Pending";

    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
