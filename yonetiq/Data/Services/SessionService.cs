using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

/// <summary>
/// Scoped — Blazor Server devresi (circuit) başına aktif kullanıcı oturum bilgilerini saklar.
/// RAM tabanlı bir state yönetimidir; veritabanı ile doğrudan ilişkili değildir.
/// </summary>
public class SessionService
{
    private SessionInfo _info = new();

    /// <summary>
    /// Mevcut oturumdaki aktif kullanıcı bilgilerini döner.
    /// </summary>
    public SessionInfo ActiveUser => _info;

    /// <summary>
    /// Kullanıcının sisteme başarıyla giriş yapıp yapmadığını belirtir.
    /// </summary>
    public bool IsLoggedIn => _info.IsLoggedIn;

    /// <summary>
    /// Belirtilen kullanıcı bilgileriyle yeni bir oturum başlatır.
    /// </summary>
    public void Login(User user)
    {
        _info = new SessionInfo
        {
            UserId           = user.Id,
            FullName         = user.FullName,
            Role             = user.RoleName ?? "Personel",
            IsLoggedIn       = true,
            Email            = user.Email,
            TelegramChatId   = user.TelegramChatId,
            DepartmentLookupId = user.DepartmentLookupId
        };
    }

    /// <summary>
    /// Mevcut kullanıcıyı sistemden çıkarır ve oturum bilgisini sıfırlar.
    /// </summary>
    public void Logout()
    {
        _info = new SessionInfo();
    }
}
