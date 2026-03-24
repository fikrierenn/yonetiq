using YonetIQ.Data.Models;

namespace YonetIQ.Data.ViewModels;

/// <summary>
/// Rapor listeleme sayfası için kullanılan veri modelidir.
/// </summary>
public class ReportsIndexViewModel
{
    /// <summary>
    /// Aktif kullanıcının ID'si.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Arama çubuğuna girilen metin.
    /// </summary>
    public string SearchText { get; set; } = string.Empty;

    /// <summary>
    /// Filtreleme için seçilen kategori.
    /// </summary>
    public string SelectedCategory { get; set; } = string.Empty;

    /// <summary>
    /// Mevcut tüm kategori listesi.
    /// </summary>
    public List<string> Categories { get; set; } = [];

    /// <summary>
    /// Kullanıcının favorilerindeki raporların ID listesi.
    /// </summary>
    public HashSet<int> FavoriteReportIds { get; set; } = [];

    /// <summary>
    /// Kullanıcı ile paylaşılan raporların ID listesi.
    /// </summary>
    public HashSet<int> SharedReportIds { get; set; } = [];

    /// <summary>
    /// Sadece paylaşılan raporların gösterilip gösterilmeyeceği.
    /// </summary>
    public bool OnlyShared { get; set; }

    /// <summary>
    /// Listelenecek rapor kayıtları.
    /// </summary>
    public List<QueryRecord> Reports { get; set; } = [];
}

