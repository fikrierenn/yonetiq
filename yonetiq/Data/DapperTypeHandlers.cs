using System.Data;
using System.Globalization;
using System.Text;
using Dapper;

namespace YonetIQ.Data;

// ResultType ve SetType artık string olduğundan enum handler gerekmez.
// İleride başka enum tipleri eklenirse bu handler yeniden kullanılabilir.
public static class DapperTypeHandlers
{
    public static void Register()
    {
        // Şu an kayıtlı handler yok — gerektiğinde buraya ekle
    }
}

public class EnumStringHandler<T> : SqlMapper.TypeHandler<T> where T : struct, Enum
{
    public override void SetValue(IDbDataParameter parameter, T value)
        => parameter.Value = value.ToString();

    public override T Parse(object value)
    {
        var raw = value?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return default;
        }

        if (Enum.TryParse<T>(raw, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        var normalizedRaw = NormalizeToken(raw);
        foreach (var enumName in Enum.GetNames<T>())
        {
            if (NormalizeToken(enumName) == normalizedRaw &&
                Enum.TryParse<T>(enumName, ignoreCase: true, out var normalizedParsed))
            {
                return normalizedParsed;
            }
        }

        return default;
    }

    private static string NormalizeToken(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD).ToLowerInvariant();
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}
