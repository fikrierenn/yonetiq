using System.ComponentModel.DataAnnotations;

namespace YonetIQ.Data.Models;

/// <summary>
/// Birbirleriyle ilişkili birden fazla Sorgunun (QueryRecord) bir araya getirilerek oluşturulduğu Set/Pano (Örn: Yönetici Dashboard'u).
/// Bir set'in içine birden fazla SetQuery kartı eşleştirilip menüde tekilleştirilir.
/// </summary>
public class QuerySet
{
    /// <summary>
    /// Sorgu setinin / Dashboard'un benzersiz kimlik numarası (Primary Key).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Menüde ve ekran tepesinde görünecek Dashboard / Set adı.
    /// </summary>
    [Required(ErrorMessage = "Set adı zorunludur")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kullanıcılar için kısa bir rehber veya bu setin neyi gösterdiğine dair açıklama.
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Bu setin bir Dashboard mi, Rapor mu olduğunu işaretleyen tip belirteci.
    /// Değeri veritabanındaki "SetType" Lookups grubundan gelir. (Örn: "Dashboard", "Report", "KpiPanel", "Custom")
    /// </summary>
    public string Type { get; set; } = "Report";

    /// <summary>
    /// Sol Navigation Menüde gösterilecek Bootstrap Icons ikon adı.
    /// </summary>
    [MaxLength(100)]
    public string Icon { get; set; } = "grid";

    /// <summary>
    /// Bayrak aktifleştirildiğinde, bu Dashboard direkt olarak sol ana menü çubuğuna (NavMenu) yerleşir.
    /// </summary>
    public bool ShowInNavMenu { get; set; } = true;

    /// <summary>
    /// Sol menüde hangi sıralama numarasında çıkacağını belirler (Küçükten büyüğe).
    /// </summary>
    public int NavMenuOrder { get; set; } = 0;

    /// <summary>
    /// Setin sistemde ilk defa tanımlandığı tarih.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
