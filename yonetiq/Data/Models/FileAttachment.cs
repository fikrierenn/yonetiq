using System;
using System.ComponentModel.DataAnnotations;

namespace YonetIQ.Data.Models;

/// <summary>
/// Sistemdeki nesnelere (Görev, Toplantı, Karar vb.) iliştirilen dosyaları temsil eden modeldir.
/// </summary>
public class FileAttachment : BaseEntity
{
    /// <summary>
    /// Dosyanın orijinal adı (Örn: rapor.pdf).
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Dosyanın depolama alanındaki yolu veya benzersiz adı.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Dosyanın MIME tipi (Örn: application/pdf, image/png).
    /// </summary>
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Dosyanın byte cinsinden boyutu.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Dosyanın iliştirildiği nesnenin tipi (Örn: Task, Meeting, Decision).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RelatedEntityType { get; set; } = string.Empty;

    /// <summary>
    /// Dosyanın iliştirildiği nesnenin ID'si.
    /// </summary>
    public int RelatedEntityId { get; set; }

    /// <summary>
    /// Dosyayı yükleyen kullanıcının ID'si.
    /// </summary>
    public int? CreatedByUserId { get; set; }
}
