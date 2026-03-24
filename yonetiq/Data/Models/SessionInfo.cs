namespace YonetIQ.Data.Models;

/// <summary>
/// Sisteme giriş yapan (Login olan) aktif kullanıcının bilgilerini RAM (Scoped Service) üzerinde tutan oturum nesnesi.
/// Veritabanı tablosu değildir. Her HTTP Request / SignalR devresinde kullanıcı bilgilerini taşır.
/// </summary>
public class SessionInfo
{
    /// <summary>Giriş yapan kullanıcının veritabanındaki ID numarası.</summary>
    public int UserId { get; set; }

    /// <summary>Arayüzde (Navbar vb.) göstermek üzere kullanıcının tam adı ve soyadı.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Yetki kontrolleri (Authorization) için kullanıcının sahip olduğu rol adı (Örn: "Admin").</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Kullanıcının sistemde geçerli bir doğrulamasının (Authentication) olup olmadığını tutan bayrak.</summary>
    public bool IsLoggedIn { get; set; }

    /// <summary>Kullanıcının e-posta adresi (rapor gönderimi vb. için).</summary>
    public string? Email { get; set; }

    /// <summary>Kullanıcının Telegram Chat ID'si (Telegram bildirimleri için).</summary>
    public string? TelegramChatId { get; set; }

    /// <summary>Kullanıcının bağlı olduğu departmanın Lookups ID'si. Departman bazlı erişim kontrolünde kullanılır.</summary>
    public int? DepartmentLookupId { get; set; }
}
