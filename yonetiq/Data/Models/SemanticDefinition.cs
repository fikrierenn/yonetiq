namespace YonetIQ.Data.Models;

/// <summary>
/// İş terimleri ile veritabanı şeması arasındaki anlam köprüsü.
/// AI'ın NL→SQL ve rapor açıklama skill'lerinde kullanılır.
/// </summary>
public class SemanticDefinition
{
    public int Id { get; set; }

    /// <summary>Tanım tipi: table, column, metric, relationship</summary>
    public string TermType { get; set; } = string.Empty;

    /// <summary>Tablo adı (TermType=table veya column ise)</summary>
    public string? TableName { get; set; }

    /// <summary>Kolon adı (TermType=column ise)</summary>
    public string? ColumnName { get; set; }

    /// <summary>İş terimi (ör: "Aktif Çalışan Sayısı")</summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>Açıklama</summary>
    public string? Description { get; set; }

    /// <summary>SQL ifadesi (TermType=metric ise)</summary>
    public string? SqlExpression { get; set; }

    /// <summary>Alternatif isimler (virgülle ayrılmış)</summary>
    public string? Aliases { get; set; }

    /// <summary>Veri kaynağı ID (DataSources tablosu)</summary>
    public int? DataSourceId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
