namespace YonetIQ.Data.Models;

/// <summary>
/// Sistemde kimin, ne zaman, hangi işlemi yaptığını takip etmek için tasarlanan denetim (Audit) izi varlık sınıfı.
/// Raporlama, güvenlik ve hata tespiti (Troubleshooting) için kritiktir.
/// </summary>
public class SystemLog
{
    /// <summary>Log kaydının benzersiz kimlik numarası (Primary Key).</summary>
    public int Id { get; set; }

    /// <summary>İşlemi gerçekleştiren kullanıcının ID'si. Eğer sistemin kendisi bir işlem yaptıysa (Örn: BackgroundWorker) null olabilir.</summary>
    public int? UserId { get; set; }

    /// <summary>Arama kolaylığı için işlemi yapan kullanıcının silinme ihtimaline karşı anlık düz metin kopyası.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>İşlemin hangi modülde, tabloda veya sayfada gerçekleştiği (Örn: "TaskItem", "Meeting", "System").</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>Yapılan eylemin tipi (Örn: "Create", "Delete", "StatusUpdate").</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>İşlem hakkında detaylı bilgi, JSON serileştirilmiş eski/yeni değerler veya sistem hata mesajı eklenebilir.</summary>
    public string? Details { get; set; }

    /// <summary>Logun/Eylemin gerçekleştiği kesin tarih ve saat (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
