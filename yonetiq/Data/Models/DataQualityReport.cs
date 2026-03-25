namespace YonetIQ.Data.Models;

/// <summary>
/// Veri kaynağının kalite skorunu temsil eder (0-100).
/// SchemaDiscoveryService ve DataSourceService tarafından kullanılır.
/// </summary>
public class DataQualityReport
{
    public int DataSourceId { get; set; }
    public bool HasSalesTable { get; set; }
    public bool HasProductTable { get; set; }
    public bool HasStoreTable { get; set; }

    /// <summary>Kritik alanlardaki null oranı (%). -1 = test edilemedi.</summary>
    public decimal NullRateEstimate { get; set; }

    /// <summary>Genel kalite skoru (0-100)</summary>
    public int Score { get; set; }

    public string ScoreLabel => Score >= 80 ? "İyi" : Score >= 50 ? "Orta" : "Zayıf";
}
