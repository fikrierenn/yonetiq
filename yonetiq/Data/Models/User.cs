namespace YonetIQ.Data.Models;

/// <summary>
/// Sistemdeki kullanıcıları temsil eden ana varlık sınıfı.
/// Kimlik doğrulama, yetkilendirme ve sistem içindeki tüm eylemler (görev atama vs.) bu varlık üzerinden yürütülür.
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Kullanıcının sistemde görünen tam adı ve soyadı.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Kullanıcının sisteme giriş yaparken kullandığı e-posta adresi. Benzersiz olmalıdır.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Kullanıcının sistemdeki rolünü (Admin, Personel vb.) belirten tanım ID'si. (Tanimlar tablosu ile ilişkili)
    /// </summary>
    public int RoleLookupId { get; set; }

    /// <summary>
    /// Kullanıcının hesabının aktif olup olmadığını belirtir. Pasif hesaplar sisteme giriş yapamaz.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Kullanıcının şifresinin güvenli bir şekilde saklanmış (hashlenmiş) hali.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Şifrenin güvenliğini artırmak için hashleme sırasında kullanılan rastgele üretilmiş tuz değeri.
    /// </summary>
    public string? PasswordSalt { get; set; }

    /// <summary>
    /// Kullanıcının bağlı bulunduğu departmanın tanım ID'si. (Tanimlar tablosu ile ilişkili)
    /// Opsiyoneldir.
    /// </summary>
    public int? DepartmentLookupId { get; set; }

    /// <summary>
    /// Rol adı (Lookups.Value). Dapper JOIN sorgularından doldurulur; DB kolonu değildir.
    /// EF migration bu alanı yok sayar (NotMapped).
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? RoleName { get; set; }

    /// <summary>
    /// Telegram Login Widget aracılığıyla bağlanan Telegram hesabının numeric kullanıcı ID'si.
    /// Rapor iletme ve bildirim gönderimi için kullanılır.
    /// </summary>
    public string? TelegramChatId { get; set; }

    /// <summary>
    /// Şifre sıfırlama e-postasında gönderilen tek kullanımlık token (64 char hex).
    /// </summary>
    public string? PasswordResetToken { get; set; }

    /// <summary>
    /// Şifre sıfırlama tokenının geçerlilik süresi (UTC). Token bu süreden sonra kullanılamaz.
    /// </summary>
    public DateTime? PasswordResetExpiry { get; set; }
}
