namespace YonetIQ.Data.Models;

/// <summary>
/// Dapper ile veritabanından dinamik olarak çalıştırılan SQL sorgusunun sonucunu (DataRow'ları) tutan sarmalayıcı (wrapper) sınıf.
/// Veritabanı tablosu değildir, sadece runtime (çalışma zamanı) veri taşıma objesidir.
/// </summary>
public class QueryResult
{
    /// <summary>Sorgunun başlığı veya adı (opsiyonel).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Dönen SQL sonucundaki sütun (kolon) isimlerinin listesi.</summary>
    public List<string> Columns { get; set; } = [];

    /// <summary>Her bir satırın sözlük (Dictionary) formatında tutulduğu veri kümesi. Key = Kolon Adı, Value = Veri.</summary>
    public List<Dictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>Sorgu çalıştırılırken bir SQL veya Timeout hatası oluşursa hatanın detayını tutar.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Eğer ErrorMessage boşsa işlemin başarılı olduğunu belirten pratik (helper) özellik.</summary>
    public bool IsSuccess => ErrorMessage is null;

    /// <summary>Sorgudan kaç satır veri döndüğünü belirten pratik özellik.</summary>
    public int RowCount => Rows.Count;
}

/// <summary>
/// ApexCharts gibi grafik kütüphanelerinde tek bir çizgi/bar (Seri) içerisindeki veri noktasını (X ve Y koordinatı) temsil eder.
/// </summary>
public class ChartRow
{
    public string X { get; set; } = "";
    public decimal Y { get; set; }
}

/// <summary>
/// ApexCharts kütüphanesinde çoklu grafik çizimi için veri serilerini (Örn: "2025 Satışları Serisi", "2026 Satışları Serisi") sarmalar.
/// </summary>
public class ChartSeries
{
    public string Name { get; set; } = "";
    public List<ChartRow> Rows { get; set; } = [];
}
