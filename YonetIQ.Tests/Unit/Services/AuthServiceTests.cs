using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Services;

namespace YonetIQ.Tests.Unit.Services;

public class AuthServiceTests
{
    private static AuthService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=Test;Trusted_Connection=true;"
            })
            .Build();
        return new AuthService(config, null!);
    }

    [Fact]
    public void HashPassword_ReturnsNonEmptyHashAndSalt()
    {
        var svc = CreateService();
        var (hash, salt) = svc.HashPassword("Test1234!");
        Assert.False(string.IsNullOrEmpty(hash));
        Assert.False(string.IsNullOrEmpty(salt));
    }

    [Fact]
    public void HashPassword_SamePassword_DifferentSalt()
    {
        var svc = CreateService();
        var (hash1, salt1) = svc.HashPassword("Test1234!");
        var (hash2, salt2) = svc.HashPassword("Test1234!");
        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var svc = CreateService();
        var (hash, salt) = svc.HashPassword("Admin1234!");
        Assert.True(svc.VerifyPassword("Admin1234!", hash, salt));
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var svc = CreateService();
        var (hash, salt) = svc.HashPassword("Admin1234!");
        Assert.False(svc.VerifyPassword("WrongPass!", hash, salt));
    }

    [Fact]
    public void VerifyPassword_EmptyPassword_ReturnsFalse()
    {
        var svc = CreateService();
        var (hash, salt) = svc.HashPassword("Admin1234!");
        Assert.False(svc.VerifyPassword("", hash, salt));
    }

    [Fact]
    public void HashPassword_ProducesBase64Output()
    {
        var svc = CreateService();
        var (hash, salt) = svc.HashPassword("test");
        // Base64 doğrulaması: Convert.FromBase64String hata vermemeli
        var hashBytes = Convert.FromBase64String(hash);
        var saltBytes = Convert.FromBase64String(salt);
        Assert.Equal(32, hashBytes.Length); // HMACSHA256 = 32 byte
        Assert.Equal(32, saltBytes.Length); // 32 byte random salt
    }

    [Fact]
    public void VerifyPassword_CaseSensitive()
    {
        var svc = CreateService();
        var (hash, salt) = svc.HashPassword("Admin1234!");
        Assert.False(svc.VerifyPassword("admin1234!", hash, salt));
    }
}
