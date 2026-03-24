namespace YonetIQ.Data.Models;

/// <summary>
/// Kullanıcıya özel uygulama içi bildirim.
/// Görev atama, yeni mesaj, hatırlatma gibi olaylar için üretilir.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>Bildirimin gönderildiği kullanıcının ID'si.</summary>
    public int UserId { get; set; }

    /// <summary>Bildirim tipi (plain string, enum yok) — NotificationTypes sabitlerini kullan.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Bildirim başlığı (kısa, tek satır).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Bildirim mesajı (detay metni).</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Tıklandığında yönlendirilecek uygulama içi URL (ör. "/gorevler"). Null ise yönlendirme yok.</summary>
    public string? RelatedUrl { get; set; }

    /// <summary>Kullanıcı bildirimi okuduysa true.</summary>
    public bool IsRead { get; set; }
}
