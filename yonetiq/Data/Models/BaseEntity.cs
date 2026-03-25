namespace YonetIQ.Data.Models;

/// <summary>
/// Tüm veri modellerinin türetileceği temel sınıftır.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
