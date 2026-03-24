namespace YonetIQ.Data.Models;

/// <summary>
/// Dashboard (QuerySet) ile içerisindeki Raporlar (QueryRecord) arasındaki çoktan-çoka (M:N) ilişki tablosu.
/// Hangi raporun hangi dashboard'da, hangi sırada ve hangi genişlikte gösterileceğini tutar.
/// </summary>
public class SetQuery
{
    /// <summary>Eşleştirme kaydının benzersiz kimlik numarası (Primary Key).</summary>
    public int Id { get; set; }

    /// <summary>Bu eşleştirmenin ait olduğu Dashboard'un ID'si (QuerySet tablosuna FK).</summary>
    public int SetId { get; set; }

    /// <summary>Dashboard içerisinde gösterilecek olan Raporun ID'si (QueryRecord tablosuna FK).</summary>
    public int QueryId { get; set; }

    /// <summary>Aynı Dashboard içerisinde birden fazla rapor varsa, bu raporun kaçıncı sırada gösterileceği.</summary>
    public int OrderIndex { get; set; } = 0;

    /// <summary>MudBlazor grid sistemindeki (MudGrid) 12'lik düzene göre bu raporun ne kadar genişlik kaplayacağı (1-12). Örn: 6 = yarım ekran.</summary>
    public int WidthMd { get; set; } = 6;
}
