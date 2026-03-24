namespace YonetIQ.Data.Models;

/// <summary>
/// Kullanıcıların, hızlı erişim için yıldızladıkları (favoriye aldıkları) rapor/sorguları temsil eden ilişki tablosu.
/// Many-to-Many bir ara tablodur.
/// </summary>
public class ReportFavorite
{
    /// <summary>
    /// Raporu favorilerine alan kullanıcının ID'si (User tablosuna FK).
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Favorilere eklenen raporun/sorgunun ID'si (QueryRecord tablosuna FK).
    /// </summary>
    public int QueryId { get; set; }

    /// <summary>
    /// Sistemin, kullanıcının o raporu hangi tarihte favoriye aldığını tuttuğu zaman damgası.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
