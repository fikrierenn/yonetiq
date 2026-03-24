using System.ComponentModel.DataAnnotations;

namespace YonetIQ.Data.Models;

/// <summary>
/// Sistemdeki kullanıcıların üzerine atanan işleri (görevleri) temsil eden varlık sınıfı.
/// Kişisel notlardan genel projelere kadar her türlü iş kalemi bu sınıfta tutulacaktır.
/// </summary>
public class TaskItem : BaseEntity
{
    /// <summary>
    /// Görevin başlığı veya kısa özeti. Ekranda kullanıcıya gösterilen ana metindir.
    /// </summary>
    [Required(ErrorMessage = "Gorev basligi zorunludur")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Görevin nasıl yapılacağı veya detaylarını içeren geniş açıklama metni.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Görevin atandığı kişinin (veya grubun) düz metin olarak adı. Sadece isim olarak kullanılacağı eski sistemden kalma veya geçici senaryolar için.
    /// </summary>
    [MaxLength(100)]
    public string AssigneeName { get; set; } = string.Empty;

    /// <summary>
    /// Görevin atandığı sistem kullanıcısının ID'si (User tablosu ile ilişkili). Opsiyoneldir.
    /// </summary>
    public int? AssigneeId { get; set; }

    /// <summary>
    /// Sisteme (veya kişiye) bu görevin ilk atandığı tarih (genellikle bugün).
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.Today;

    /// <summary>
    /// Görevin tamamlanmasının beklendiği/hedeflendiği son teslim tarihi. (Deadline)
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Görevin öncelik durumunu (Düşük, Normal, Yüksek vb.) tutan tanım ID'si. (Lookups tablosu ile ilişkili)
    /// </summary>
    public int PriorityLookupId { get; set; }

    /// <summary>
    /// Görevin mevcut durumunu (Bekliyor, Devam Ediyor, Tamamlandı vb.) tutan tanım ID'si. (Lookups tablosu ile ilişkili)
    /// </summary>
    public int StatusLookupId { get; set; }

    /// <summary>
    /// Görevin kaynağını belirten entity tipi (Örn: Decision, Meeting).
    /// </summary>
    [MaxLength(50)]
    public string? SourceEntityType { get; set; }

    /// <summary>
    /// Görevin kaynağını belirten entity ID'si.
    /// </summary>
    public int? SourceEntityId { get; set; }
}
