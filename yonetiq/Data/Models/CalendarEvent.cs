namespace YonetIQ.Data.Models;

/// <summary>
/// Takvim takibi yapan UI bileşenine (Örn: MudCalendar) farklı tablolardan (Toplantı, Görev, Özel Gün) gelen verileri
/// tek tip (uniform) hale getirerek sunmaya yarayan yardımcı transfer (DTO) nesnesi. Veritabanı tablosu değildir.
/// </summary>
public class CalendarEvent
{
    /// <summary>Olayın takvimde işaretleneceği zaman damgası.</summary>
    public DateTime Date { get; set; }

    /// <summary>Olayın hangi veri tipinden geldiğini belirtir (Decision, Meeting, TaskItem, SpecialDay).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Takvim üzerinde görünecek ana metin.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Olay tıklandığında gösterilebilecek alt açıklama.</summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>Bu olayın veritabanındaki orijinal tablosundaki benzersiz kimlik numarası (Id).</summary>
    public int ReferenceId { get; set; }

    /// <summary>Varsa olaya ait dış web bağlantısı (Örn: Toplantı Zoom linki).</summary>
    public string? Link { get; set; }

    /// <summary>Sıralama veya renklendirme için kullanılabilecek öncelik seviyesi.</summary>
    public int Priority { get; set; }
}
