using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YonetIQ.Data.Models;

/// <summary>
/// Toplantılar sırasında alınan kararları temsil eden varlık sınıfı.
/// Kararlar, sadece bilgi amaçlı kalabileceği gibi daha sonradan bir 'TaskItem' (Görev) nesnesine de dönüşebilir.
/// </summary>
public class Decision : BaseEntity
{
    /// <summary>
    /// Bu kararın alındığı toplantının benzersiz ID'si (Meeting tablosuna Foreign Key).
    /// </summary>
    public int MeetingId { get; set; }

    /// <summary>
    /// Alınan kararın tam içeriği veya metni.
    /// </summary>
    [Required(ErrorMessage = "Karar icerigi zorunludur")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Kararı uygulamakla veya takip etmekle sorumlu olan kişinin düz metin ismi.
    /// </summary>
    [MaxLength(100)]
    public string AssigneeName { get; set; } = string.Empty;

    /// <summary>
    /// Kararı uygulamakla sorumlu olan kayıtlı sistem kullanıcısının ID'si (User tablosuna FK).
    /// </summary>
    public int? AssigneeId { get; set; }

    /// <summary>
    /// Kararın uygulanması için belirlenen hedef bitiş tarihi.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Kararın şu andaki durumunu (Bekliyor, Uygulandı, İptal vb.) belirten tanım ID'si. (Lookups tablosuna FK).
    /// </summary>
    public int StatusLookupId { get; set; }

    /// <summary>
    /// Kararın öncelik seviyesini (Düşük, Normal, Yüksek, Kritik) belirten tanım ID'si. (Lookups tablosuna FK).
    /// </summary>
    public int? PriorityLookupId { get; set; }

    /// <summary>
    /// Bu kararın resmi bir operasyonel "Görevi" tetikleyip tetiklemediğini (TaskItem üretilip üretilmediğini) takip eden bayrak.
    /// </summary>
    public bool IsConvertedToTask { get; set; }

    /// <summary>
    /// Eğer karat bir göreve dönüştüyse, o görevin ID'si burada saklanır. Karar ile TaskItem'ı bağlamak için kullanılır.
    /// </summary>
    public int? LinkedTaskId { get; set; }

    /// <summary>
    /// Arayüzde (UI) gösterim kolaylığı için toplantının başlığını tutan eşlenmemiş (veritabanında olmayan) alan.
    /// </summary>
    [NotMapped]
    public string? MeetingTitle { get; set; }

    /// <summary>
    /// Arayüzde gösterim kolaylığı için toplantının tarihini tutan eşlenmemiş alan.
    /// </summary>
    [NotMapped]
    public DateTime? MeetingDate { get; set; }
}
