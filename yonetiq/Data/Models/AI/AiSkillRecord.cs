namespace YonetIQ.Data.Models.AI;

/// <summary>
/// AiSkillDefinitions tablosundaki bir skill kaydını temsil eder.
/// SkillPersistenceService bu modeli kullanarak DB'den skill yükler/kaydeder.
/// </summary>
public class AiSkillRecord
{
    public int Id { get; set; }
    public string SkillId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TriggerMode { get; set; } = "Reactive";
    public decimal Temperature { get; set; } = 0.2m;
    public int MaxTokenInput { get; set; } = 4000;
    public int MaxOutputLength { get; set; } = 1500;
    public string? SystemPromptOverride { get; set; }
    public string? UserPromptOverride { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Version { get; set; } = 1;
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
