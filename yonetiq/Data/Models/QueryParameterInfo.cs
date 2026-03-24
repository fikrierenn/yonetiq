namespace YonetIQ.Data.Models;

/// <summary>
/// SQL sorgusunda tespit edilen bir kullanıcı parametresinin (@@Param) meta bilgilerini tutar.
/// SQL'deki DECLARE satırlarından tip, SQL yorumlarından dropdown seçenekleri otomatik çözümlenir.
/// </summary>
public class QueryParameterInfo
{
    /// <summary>Parametre adı — @ işareti olmadan (örn: "BaslangicAy").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// UI'da gösterilecek input tipi:
    /// "text" | "number" | "number-decimal" | "date" | "datetime" | "select" | "yesno"
    /// </summary>
    public string InputType { get; set; } = "text";

    /// <summary>InputType="select" ise dropdown seçenekleri (options=A,B,C yorumundan).</summary>
    public List<string> Options { get; set; } = [];

    /// <summary>SQL'deki DECLARE satırından veya DEFAULT değerinden alınan başlangıç değeri.</summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>Kullanıcının girdiği değer.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>SQL'deki ham tip adı (örn: "NVARCHAR", "INT", "DATE"). Bilgi amaçlı.</summary>
    public string SqlType { get; set; } = string.Empty;
}
