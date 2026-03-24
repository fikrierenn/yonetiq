using System.Security.Cryptography;
using System.Text;

namespace YonetIQ.Data.Infrastructure;

/// <summary>
/// AES-256-CBC algoritmasıyla metin şifreleme ve çözme işlemlerini yürütür.
/// Veri kaynağı bağlantı stringleri bu sınıf aracılığıyla korunur.
/// </summary>
public static class AesEncryption
{
    /// <summary>
    /// Verilen açık metni AES-256 ile şifreler.
    /// </summary>
    /// <param name="plainText">Şifrelenecek metin.</param>
    /// <param name="base64Key">32 baytlık Base64 kodlu şifreleme anahtarı.</param>
    /// <returns>IV + şifreli veri içeren Base64 dizisi.</returns>
    public static string Encrypt(string plainText, string base64Key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Key);

        var key = Convert.FromBase64String(base64Key);
        if (key.Length != 32)
            throw new ArgumentException("Şifreleme anahtarı 32 bayt (256 bit) olmalıdır.", nameof(base64Key));

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV(); // Her şifrelemede benzersiz IV üretilir

        using var ms = new MemoryStream();
        // IV'yi ciphertext'in başına yaz (şifre çözme sırasında okunacak)
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// AES-256 ile şifrelenmiş bir metni çözer.
    /// </summary>
    /// <param name="cipherText">IV + şifreli veri içeren Base64 dizisi.</param>
    /// <param name="base64Key">32 baytlık Base64 kodlu şifreleme anahtarı.</param>
    /// <returns>Orijinal açık metin.</returns>
    public static string Decrypt(string cipherText, string base64Key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Key);

        var key = Convert.FromBase64String(base64Key);
        if (key.Length != 32)
            throw new ArgumentException("Şifreleme anahtarı 32 bayt (256 bit) olmalıdır.", nameof(base64Key));

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = key;

        // İlk 16 bayt IV'dir
        var iv = new byte[aes.IV.Length];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);
        return sr.ReadToEnd();
    }

    /// <summary>
    /// Yeni bir 256-bit (32 bayt) rastgele AES anahtarı üretir ve Base64 olarak döndürür.
    /// </summary>
    public static string GenerateKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}
