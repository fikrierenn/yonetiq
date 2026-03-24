namespace YonetIQ.Data.Models;

/// <summary>
/// Sistem genelindeki dinamik listeleri (Roller, Departmanlar, Görev Durumları vb.) tutan tanım/sözlük sınıfı.
/// Hardcoded (sabit) Enum kullanımını engelleyerek veritabanı üzerinden yönetilebilir kategoriler sunar.
/// </summary>
public class Lookup : BaseEntity
{
    /// <summary>
    /// Tanımın ait olduğu grup adı (Örn: "Role", "Department", "TaskStatus").
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// Tanımın ekranda gösterilecek metinsel değeri (Örn: "Genel Müdür", "IT Departmanı").
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Bu tanımın aktif olup olmadığını belirtir. Silmek yerine pasife çekmek, geçmiş verilerin bozulmasını engeller.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
