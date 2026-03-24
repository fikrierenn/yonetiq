namespace YonetIQ.Data.Models;

/// <summary>
/// Kullanıcıya ait kişisel notları temsil eder.
/// Hatırlatma zamanlayıcısı ve göreve dönüştürme özelliklerini destekler.
/// </summary>
public class PersonalNote : BaseEntity
{
    /// <summary>Notu oluşturan kullanıcının ID'si.</summary>
    public int UserId { get; set; }

    /// <summary>Notun kısa başlığı.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Notun detaylı içeriği.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Virgülle ayrılmış etiketler (örn: "ilaç,sağlık,randevu").</summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>Hatırlatma zamanı. Null ise hatırlatma yok.</summary>
    public DateTime? ReminderAt { get; set; }

    /// <summary>Hatırlatma tetiklenip kullanıcı tarafından kapatıldıysa true.</summary>
    public bool IsReminderDismissed { get; set; }

    /// <summary>Bu nottan dönüştürülen görevin ID'si (varsa).</summary>
    public int? LinkedTaskId { get; set; }
}
