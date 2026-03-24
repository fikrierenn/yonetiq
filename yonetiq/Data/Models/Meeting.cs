using System.ComponentModel.DataAnnotations;

namespace YonetIQ.Data.Models;

/// <summary>
/// Sistemde düzenlenen ve kararların alındığı toplantıları temsil eden varlık sınıfı.
/// İçerisinde nelerin konuşulacağı, nerede yapılacağı ve sonuçta hangi Kararların (Decision) çıktığını barındırır.
/// </summary>
public class Meeting : BaseEntity
{
    /// <summary>
    /// Toplantının konusu, başlığı veya amacı.
    /// </summary>
    [Required(ErrorMessage = "Baslik zorunludur")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Toplantının gerçekleştirileceği tarih ve saat mekanizması.
    /// </summary>
    public DateTime MeetingDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Toplantının yapılacağı fiziksel mekanın adı (Örn: Yönetim Katı Toplantı Salonu).
    /// </summary>
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Toplantı dijital ortamda (Teams, Zoom, Google Meet) gerçekleşecekse çevrimiçi bağlantı linki.
    /// </summary>
    [MaxLength(500)]
    public string? MeetingLink { get; set; }

    /// <summary>
    /// Toplantıya katılması beklenen veya katılan kişilerin isim listesi.
    /// </summary>
    [MaxLength(500)]
    public string Participants { get; set; } = string.Empty;

    /// <summary>
    /// Toplantıda görüşülecek maddelerin yer aldığı gündem içeriği metni.
    /// </summary>
    public string Agenda { get; set; } = string.Empty;

    /// <summary>
    /// Toplantı sırasındaki serbest stilde alınan notlar ve tutanaklar. (Alınan somut kararlar Kararlar listesinde tutulmalıdır).
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Tekrarlama tipi: "None", "Daily", "Weekly", "Monthly", "Yearly"
    /// </summary>
    [MaxLength(20)]
    public string RecurrenceType { get; set; } = "None";

    /// <summary>
    /// Tekrarlama aralığı (örn. her 2 haftada → RecurrenceType=Weekly, RecurrenceInterval=2)
    /// </summary>
    public int RecurrenceInterval { get; set; } = 1;

    /// <summary>
    /// Tekrarlı toplantı serisinin bitiş tarihi.
    /// </summary>
    public DateTime? RecurrenceEndDate { get; set; }

    /// <summary>
    /// Bu toplantı tekrarlı serinin bir parçasıysa, ana/ilk toplantının ID'si.
    /// </summary>
    public int? ParentMeetingId { get; set; }

    /// <summary>
    /// Toplantı dahilinde alınmış olan aksiyon ve kararların (Decision) alt listesi.
    /// </summary>
    public List<Decision> Decisions { get; set; } = [];
}
