namespace YonetIQ.Data.Models.AI;

/// <summary>
/// Kullanıcının bir AI çıktısına verdiği geri bildirim.
/// DB tablosu: AiFeedback
/// </summary>
public class AiFeedback
{
    public int Id { get; set; }

    /// <summary>İlişkili etkileşim ID'si</summary>
    public int InteractionId { get; set; }

    /// <summary>Geri bildirim veren kullanıcı ID'si</summary>
    public int UserId { get; set; }

    /// <summary>Faydalılık puanı: 1=faydasız, 2=kısmen faydalı, 3=faydalı</summary>
    public int Rating { get; set; }

    /// <summary>Kullanıcının düzeltme metni (varsa)</summary>
    public string? CorrectionText { get; set; }

    /// <summary>Öneri kabul edildi mi?</summary>
    public bool WasAccepted { get; set; }

    /// <summary>Kabul edildi ama düzenlendi mi?</summary>
    public bool WasEdited { get; set; }

    /// <summary>Oluşturulma zamanı (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
