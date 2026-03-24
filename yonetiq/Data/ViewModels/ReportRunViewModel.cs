using YonetIQ.Data.Models;

namespace YonetIQ.Data.ViewModels;

/// <summary>
/// Rapor çalıştırma ekranı ve sonuçların gösterimi için kullanılan veri modelidir.
/// </summary>
public class ReportRunViewModel
{
    /// <summary>
    /// Üzerinde çalışılan rapor tanımı.
    /// </summary>
    public QueryRecord? SelectedReport { get; set; }

    /// <summary>
    /// Ekran görünüm modu (normal, wide, full vb.).
    /// </summary>
    public string ViewMode { get; set; } = string.Empty;

    /// <summary>
    /// Rapor sonuçları içinde yapılacak arama metni.
    /// </summary>
    public string ResultSearch { get; set; } = string.Empty;

    /// <summary>
    /// Çalıştırma sırasında alınan hata mesajı (varsa).
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// İşlemin başarılı olup olmadığını kontrol eder.
    /// </summary>
    public bool Success => string.IsNullOrWhiteSpace(Error);

    /// <summary>
    /// Sorgunun ne kadar sürede çalıştığı (ms).
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Sorgunun dönerken getirdiği toplam satır sayısı.
    /// </summary>
    public int RawRowCount { get; set; }

    /// <summary>
    /// Filtreleme sonrası ekranda gösterilen satır sayısı.
    /// </summary>
    public int DisplayedRowCount { get; set; }

    /// <summary>
    /// Sorgudan gelen filtrelenmemiş ham sonuç verisi.
    /// </summary>
    public QueryResult? RawResult { get; set; }

    /// <summary>
    /// Filtreleme uygulanmış ve ekranda gösterilmeye hazır sonuç verisi.
    /// </summary>
    public QueryResult? DisplayResult { get; set; }
}

