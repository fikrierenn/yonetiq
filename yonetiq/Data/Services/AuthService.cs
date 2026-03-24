using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

public class AuthService(IConfiguration configuration, AuditService auditService) : BaseService(configuration, auditService)
{
    // ── Hash ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Verilen ham şifreyi rastgele bir tuz (salt) ile hash'ler.
    /// </summary>
    public (string Hash, string Salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(32);
        var salt = Convert.ToBase64String(saltBytes);
        var hash = ComputeHash(password, salt);
        return (hash, salt);
    }

    /// <summary>
    /// Girilen şifrenin, saklanan hash ve tuz değeriyle eşleşip eşleşmediğini doğrular.
    /// </summary>
    public bool VerifyPassword(string password, string hash, string salt)
        => ComputeHash(password, salt) == hash;

    private static string ComputeHash(string password, string salt)
    {
        var key = Encoding.UTF8.GetBytes(salt);
        var data = Encoding.UTF8.GetBytes(password);
        var hmacHash = HMACSHA256.HashData(key, data);
        return Convert.ToBase64String(hmacHash);
    }

    /// <summary>
    /// E-posta ve şifre ile kullanıcı girişi yapar.
    /// Başarılıysa kullanıcı nesnesini döner; başarısızsa kullanıcı dostu bir mesaj ve hata kodu ile ServiceResult döndürür.
    /// Bu metotta iş kuralı hataları (kullanıcı yok, şifre yanlış vb.) exception fırlatmak yerine
    /// ServiceResult üzerinden yönetilir; böylece tüm servis katmanında ortak bir desen sağlanır.
    /// </summary>
    public async Task<ServiceResult<User>> LoginAsync(string email, string password)
    {
        // 1. Veritabanından kullanıcıyı getir (sadece teknik hatalar ExecuteServiceAsync içinde yönetilir)
        var dbResult = await ExecuteServiceAsync<User?>(async conn =>
        {
            return await conn.QuerySingleOrDefaultAsync<User>(@"
                SELECT u.*, l.Value AS RoleName
                FROM Users u
                LEFT JOIN Lookups l ON l.Id = u.RoleLookupId AND l.[Group] = 'Role'
                WHERE LOWER(LTRIM(RTRIM(u.Email))) = LOWER(@Email) AND u.IsActive = 1",
                new { Email = email.Trim() });
        });

        // 2. Eğer veritabanı erişiminde bir sorun olduysa, teknik hata bilgisiyle birlikte kullanıcıya genel bir mesaj ver.
        if (!dbResult.IsSuccess)
        {
            return ServiceResult<User>.Failure(
                "Giriş işlemi sırasında sistem hatası oluştu. Lütfen tekrar deneyin.",
                dbResult.ErrorCode ?? "LoginSystemError");
        }

        var user = dbResult.Data;

        // 3. Kullanıcı bulunamadı veya pasif
        if (user is null)
        {
            return ServiceResult<User>.Failure(
                "E-posta veya şifre hatalı ya da kullanıcı hesabı pasif.",
                "InvalidCredentials");
        }

        // 4. Kullanıcının şifre bilgisi eksikse (geçiş süreci / manuel müdahale vb.)
        if (string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.PasswordSalt))
        {
            return ServiceResult<User>.Failure(
                "Kullanıcı giriş bilgileri eksik. Lütfen sistem yöneticinize başvurun.",
                "InvalidCredentials");
        }

        // 5. Şifre eşleşmiyorsa giriş başarısız kabul edilir
        if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            LogAction("Authentication", "FailedLogin", new { UserId = user.Id, Email = user.Email, Reason = "IncorrectPassword" });
            return ServiceResult<User>.Failure(
                "E-posta veya şifre hatalı.",
                "InvalidCredentials");
        }

        // 6. Başarılı giriş
        LogAction("Authentication", "Login", new { UserId = user.Id, Email = user.Email });
        return ServiceResult<User>.Success(user, "Giriş başarılı.");
    }

    /// <summary>
    /// Kullanıcının mevcut şifresini doğrulayarak yeni bir şifre atar.
    /// İş kuralı hataları (kullanıcı yok, yanlış şifre vb.) ServiceResult ile bildirilir.
    /// </summary>
    public async Task<ServiceResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        // 1. Kullanıcı bilgisini getir
        var userResult = await ExecuteServiceAsync<User?>(async conn =>
        {
            return await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Id = @Id AND IsActive = 1", new { Id = userId });
        });

        if (!userResult.IsSuccess)
        {
            return ServiceResult.Failure(
                "Şifre güncelleme işlemi sırasında sistem hatası oluştu. Lütfen tekrar deneyin.",
                userResult.ErrorCode ?? "PasswordChangeSystemError");
        }

        var user = userResult.Data;
        if (user is null)
        {
            return ServiceResult.Failure(
                "Kullanıcı bulunamadı veya hesabı pasif.",
                "UserNotFound");
        }

        if (string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.PasswordSalt))
        {
            return ServiceResult.Failure(
                "Kullanıcı giriş bilgileri eksik. Lütfen sistem yöneticinize başvurun.",
                "InvalidCredentials");
        }

        if (!VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
        {
            LogAction("Authentication", "PasswordChangeFailed", new { UserId = userId });
            return ServiceResult.Failure(
                "Mevcut şifre hatalı.",
                "InvalidPassword");
        }

        // 2. Yeni şifreyi hesapla ve kaydet
        var (newHash, newSalt) = HashPassword(newPassword);
        var updateResult = await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "UPDATE Users SET PasswordHash = @Hash, PasswordSalt = @Salt WHERE Id = @Id",
                new { Hash = newHash, Salt = newSalt, Id = userId });
        });

        if (!updateResult.IsSuccess)
        {
            return ServiceResult.Failure(
                "Şifre güncellenirken sistem hatası oluştu. Lütfen tekrar deneyin.",
                updateResult.ErrorCode ?? "PasswordChangeSystemError");
        }

        LogAction("Authentication", "PasswordChanged", new { UserId = userId });
        return ServiceResult.Success("Şifreniz başarıyla güncellendi.");
    }

    /// <summary>
    /// Şifre sıfırlama akışını başlatır: token üretir, DB'ye yazar ve reset maili gönderir.
    /// Güvenlik için kullanıcı bulunmasa da başarılı mesaj döner (enumeration koruması).
    /// </summary>
    public async Task<ServiceResult> InitiatePasswordResetAsync(
        string email, EmailService emailSvc, string appBaseUrl)
    {
        var userResult = await ExecuteServiceAsync<User?>(async conn =>
            await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE LOWER(LTRIM(RTRIM(Email))) = LOWER(@Email) AND IsActive = 1",
                new { Email = email.Trim() }));

        // Kullanıcı yoksa da başarı döner (enumeration koruması)
        if (!userResult.IsSuccess || userResult.Data is null)
            return ServiceResult.Success("Kayıtlı e-posta adresinize sıfırlama bağlantısı gönderildi.");

        var user = userResult.Data;
        var token  = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiry = DateTime.UtcNow.AddHours(2);

        await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "UPDATE Users SET PasswordResetToken = @Token, PasswordResetExpiry = @Expiry WHERE Id = @Id",
                new { Token = token, Expiry = expiry, Id = user.Id });
        });

        var resetLink = $"{appBaseUrl.TrimEnd('/')}/sifre-sifirla?token={token}";
        await emailSvc.SendPasswordResetAsync(user.Email!, user.FullName, resetLink);

        LogAction("Authentication", "PasswordResetInitiated", new { UserId = user.Id });
        return ServiceResult.Success("Kayıtlı e-posta adresinize sıfırlama bağlantısı gönderildi.");
    }

    /// <summary>
    /// Token kontrolü yaparak kullanıcının şifresini günceller.
    /// </summary>
    public async Task<ServiceResult> ResetPasswordAsync(string token, string newPassword)
    {
        var userResult = await ExecuteServiceAsync<User?>(async conn =>
            await conn.QueryFirstOrDefaultAsync<User>(@"
                SELECT * FROM Users
                WHERE PasswordResetToken = @Token
                  AND PasswordResetExpiry > SYSUTCDATETIME()
                  AND IsActive = 1",
                new { Token = token }));

        if (!userResult.IsSuccess)
            return ServiceResult.Failure("Sistem hatası oluştu. Lütfen tekrar deneyin.");

        if (userResult.Data is null)
            return ServiceResult.Failure("Bağlantı geçersiz veya süresi dolmuş. Lütfen yeniden şifre sıfırlama talebinde bulunun.",
                "InvalidToken");

        var user = userResult.Data;
        var (newHash, newSalt) = HashPassword(newPassword);

        await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(@"
                UPDATE Users SET
                    PasswordHash = @Hash,
                    PasswordSalt = @Salt,
                    PasswordResetToken  = NULL,
                    PasswordResetExpiry = NULL
                WHERE Id = @Id",
                new { Hash = newHash, Salt = newSalt, Id = user.Id });
        });

        LogAction("Authentication", "PasswordReset", new { UserId = user.Id });
        return ServiceResult.Success("Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.");
    }
}
