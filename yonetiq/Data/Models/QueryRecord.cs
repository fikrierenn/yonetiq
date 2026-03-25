using System.ComponentModel.DataAnnotations;

namespace YonetIQ.Data.Models;

/// <summary>
/// Veritabanından verilerin nasıl çekileceğini, nasıl görselleştirileceğini barındıran Rapor / Sorgu kayıtlarıdır.
/// Sistemin en güçlü özelliği olan "Dinamik Sorgu" altyapısının temel yapıtaşıdır.
/// </summary>
public class QueryRecord : BaseEntity
{
    /// <summary>
    /// Raporun menüde, ekranda görünecek insan okuyabilir başlığı.
    /// </summary>
    [Required(ErrorMessage = "Sorgu adi zorunludur")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Bu sorgunun ne amaca hizmet ettiğiyle veya veriyi nasıl getirdiğiyle ilgili detaylı açıklama.
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Raporu çalıştırmak / getirmek için SQL Server üzerinde çalıştırılacak T-SQL scriptinin tamamı. (SELECT veya EXEC)
    /// </summary>
    [Required(ErrorMessage = "SQL sorgusu zorunludur")]
    public string SqlContent { get; set; } = string.Empty;

    /// <summary>
    /// Veritabanından dönen satırların (DataRow) UI üzerinde nasıl render edileceğini belirleyen tip belirteci.
    /// Değeri veritabanındaki "ResultType" Lookups grubundan gelir. (Örn: "Table", "BarChart", "KpiCard")
    /// </summary>
    public string ResultType { get; set; } = "Table";

    /// <summary>
    /// Bu sorgunun Dashboard'da mı gösterileceğini, normal bir Rapor sayfasında mı gösterileceğini (veya her ikisi) tutan tanım (Lookups) ID'si.
    /// </summary>
    public int? UsagePurposeLookupId { get; set; }

    /// <summary>
    /// KPI veya Grafik sonuç tipleri için kullanılacak olan HEX formatındaki CSS renk kodu (örn: "#d32f2f").
    /// </summary>
    [MaxLength(20)]
    public string Color { get; set; } = "#1976d2";

    /// <summary>
    /// ResultType = "PivotTable" seçildiyse: Pivot formatında SATIR (Y-Axis) olarak kullanılacak olan sütun ismi.
    /// </summary>
    [MaxLength(100)]
    public string? PivotRowColumn { get; set; }

    /// <summary>
    /// ResultType = "PivotTable" seçildiyse: Pivot formatında SÜTUN (X-Axis) olarak kullanılacak olan sütun ismi.
    /// </summary>
    [MaxLength(100)]
    public string? PivotColumnColumn { get; set; }

    /// <summary>
    /// ResultType = "PivotTable" seçildiyse: Pivot kesişim noktalarına yazılacak/hesaplanacak DEĞER sütunu ismi.
    /// </summary>
    [MaxLength(100)]
    public string? PivotValueColumn { get; set; }

    /// <summary>
    /// Bu raporun veri çekeceği sunucunun ID'si (DataSources tablosu).
    /// Null ise uygulamanın varsayılan bağlantı dizisi kullanılır.
    /// </summary>
    public int? DataSourceId { get; set; }

    /// <summary>
    /// True ise INSERT/UPDATE/DELETE/MERGE komutlarının bu sorgunun SQL içeriğinde
    /// çalıştırılmasına izin verilir (DML/Uzman Modu).
    /// DDL komutları (DROP, TRUNCATE, ALTER vb.) her zaman engellenir.
    /// </summary>
    public bool AllowDml { get; set; } = false;
}
