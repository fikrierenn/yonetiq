using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Kullanıcı sorgusundaki iş terimlerini SemanticDefinitions tablosundan çözümler.
/// ContextBuilder bu servisi kullanarak NlToSql prompt'una zenginleştirilmiş bağlam ekler.
/// </summary>
public class SemanticEnricher(SemanticService semanticSvc)
{
    /// <summary>
    /// Kullanıcı girdisindeki iş terimlerini eşleştirip prompt'a eklenecek
    /// "İş Terimleri Sözlüğü" metni üretir.
    /// </summary>
    public async Task<string> EnrichAsync(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput)) return string.Empty;

        var result = await semanticSvc.SearchAsync(userInput);
        if (!result.IsSuccess || result.Data is null || result.Data.Count == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("İş Terimleri Sözlüğü:");
        foreach (var term in result.Data)
        {
            sb.Append($"  • {term.BusinessName}");
            if (!string.IsNullOrEmpty(term.Description))
                sb.Append($" — {term.Description}");
            if (!string.IsNullOrEmpty(term.SqlExpression))
                sb.Append($" | SQL: {term.SqlExpression}");
            if (!string.IsNullOrEmpty(term.TableName))
                sb.Append($" | Tablo: {term.TableName}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>Belirli bir iş terimini arar.</summary>
    public async Task<SemanticDefinition?> ResolveTermAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return null;
        var result = await semanticSvc.SearchAsync(term);
        return result.IsSuccess ? result.Data?.FirstOrDefault() : null;
    }
}
