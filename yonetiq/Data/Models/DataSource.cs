using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YonetIQ.Data.Models;

/// <summary>
/// Sistemdeki rapor ve sorguların çalıştırılacağı veritabanı sunucu kayıtlarını temsil eder.
/// Bağlantı stringleri AES-256 ile şifreli olarak saklanır.
/// </summary>
public class DataSource : BaseEntity
{
    /// <summary>
    /// Veri kaynağının kullanıcıya gösterilen adı. (Örn: "ERP Sunucusu", "Analitik DW")
    /// </summary>
    [Required(ErrorMessage = "Veri kaynağı adı zorunludur")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Bu veri kaynağının ne amaçla kullanıldığına dair açıklama.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Veritabanı sunucu tipi. Kabul edilen değerler: MSSQL, PostgreSQL, MySQL.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ServerType { get; set; } = "MSSQL";

    /// <summary>
    /// AES-256 ile şifrelenmiş bağlantı dizisi. Veritabanına sadece şifreli hali yazılır.
    /// </summary>
    [Required]
    public string EncryptedConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Bu kaydın sistemin varsayılan veri kaynağı olup olmadığını belirtir.
    /// Sadece bir kayıt aynı anda IsDefault=true olabilir.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Kaydın aktif/pasif durumu. Pasif kaynaklar seçim listesinde görünmez.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Bu kaydı oluşturan kullanıcının Id'si.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>
    /// Son güncelleme zaman damgası.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ─── Runtime-only (NotMapped) ────────────────────────────────────────────

    /// <summary>
    /// Form üzerinde geçici olarak tutulan açık metin bağlantı dizisi.
    /// Veritabanına yazılmaz; sadece şifreleme/test işlemleri sırasında kullanılır.
    /// </summary>
    [NotMapped]
    public string? PlainConnectionString { get; set; }
}
