namespace YonetIQ.Data.Infrastructure;

/// <summary>
/// Lookups tablosunda kullanılan grup ve değer adlarının sabitleridir.
/// Sihirli string tekrarını önler; değişiklik tek noktadan yapılır.
/// Veritabanındaki Lookup kayıtlarının Value alanı bu sabitlerle eşleşmelidir.
/// </summary>
public static class LookupConstants
{
    /// <summary>Görev durumu grubu (Bekliyor, Tamamlandi vb.)</summary>
    public const string GroupTaskStatus = "TaskStatus";

    /// <summary>Karar durumu grubu</summary>
    public const string GroupDecisionStatus = "DecisionStatus";

    /// <summary>Görev önceliği grubu (Normal, Yüksek vb.)</summary>
    public const string GroupTaskPriority = "TaskPriority";

    /// <summary>Departman grubu (mesaj paylaşımı vb.)</summary>
    public const string GroupDepartment = "Department";

    /// <summary>Tamamlanmış durum değeri (görev ve karar için)</summary>
    public const string ValueTamamlandi = "Tamamlandi";

    /// <summary>Bekleyen durum değeri (görev için)</summary>
    public const string ValueBekliyor = "Bekliyor";

    /// <summary>Normal öncelik değeri</summary>
    public const string ValueNormal = "Normal";

    /// <summary>İptal durum değeri (görev ve karar için)</summary>
    public const string ValueIptal = "Iptal";
}
