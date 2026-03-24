namespace YonetIQ.Data.Models;

/// <summary>
/// Raporların (QueryRecord) diğer kullanıcı veya departmanlara paylaşılmasını yetkilendiren yapı.
/// Bu sayede bir raporu yaratan kişi, onu kimlerin (User) veya hangi departmanların (DepartmentLookup) görebileceğini ayarlar.
/// </summary>
public class ReportShare : BaseEntity
{
    /// <summary>
    /// Paylaşılan raporun (Sorgunun) bağlantı ID'si (QueryRecord tablosuna FK).
    /// </summary>
    public int QueryId { get; set; }

    /// <summary>
    /// Bu paylaşımı gerçekleştiren (raporu paylaşıma açan) kullanıcının ID'si.
    /// </summary>
    public int SharedByUserId { get; set; }

    /// <summary>
    /// Rapor sadece belirli bir kişiye paylaşıldıysa o hedefin ID'si. (User tablosuna FK). Opsiyonel.
    /// </summary>
    public int? TargetUserId { get; set; }

    /// <summary>
    /// Rapor bir departmandaki herkese paylaşıldıysa o departmanın tanım ID'si. (Lookups tablosuna FK). Opsiyonel.
    /// </summary>
    public int? TargetDepartmentLookupId { get; set; }

    /// <summary>
    /// Paylaşım sırasında eklenen ekstra not veya mesaj (Örn: "Haziran verilerini kontrol ediniz").
    /// </summary>
    public string? Note { get; set; }

    // CreatedAt (BaseEntity tarafından dahil edilir)
}
