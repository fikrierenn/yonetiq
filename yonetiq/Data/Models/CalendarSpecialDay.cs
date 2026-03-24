namespace YonetIQ.Data.Models;

/// <summary>
/// Takvimde gösterilecek olan özel günleri (Resmi Tatil, Şirket İzni, Doğum Günü, vb.) temsil eden varlık sınıfı.
/// Normal görevlerden bağımsız olarak ajandada belirli günleri bloklamak için tasarlanmıştır.
/// </summary>
public class CalendarSpecialDay : BaseEntity
{
    /// <summary>
    /// Özel günün denk geldiği veya kutlanacağı temel (başlangıç) tarih.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Özel günün gösterim başlığı. Örn: "29 Ekim Cumhuriyet Bayramı".
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Bu özel günün kategorisini belirleyen tanım ID'si (Lookups tablosu ile ilişkili).
    /// </summary>
    public int TypeLookupId { get; set; }

    /// <summary>
    /// Özel gün / Tatil günü hakkındaki ekstra açıklamalar bulunuyorsa bu alanda depolanır.
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// Bu özel günün her yıl otomatik olarak tekrar edip etmeyeceğini (Doğum günleri vb.) belirten bayrak.
    /// </summary>
    public bool IsAnnualRecurring { get; set; }

    /// <summary>
    /// Tatilin veya özel günün aktif olup olmadığını takip eder. İptal durumlarında 'false' yapılarak takvimden düşürülür.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Özel gün/Tatil kaydının sisteme manuel veya otomasyonla işlendiği kayıt tarihi.
    /// </summary>
}
