using Dapper;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models.AI;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// Kullanıcı sorgularından yeni iş terimlerini keşfeder.
/// Bilinen terimlerle eşleşmeyen kelimeleri SemanticLearningCandidates'e aday olarak önerir.
/// Admin onayladığında SemanticDefinitions'a taşınır.
/// </summary>
public class SemanticDiscoveryService(
    IConfiguration config, AuditService auditSvc,
    SemanticService semanticSvc,
    ILogger<SemanticDiscoveryService> logger)
    : BaseService(config, auditSvc)
{
    // Türkçe stop-word'ler — keşiften hariç tutulacak
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ve", "veya", "ile", "için", "bir", "bu", "şu", "da", "de", "mi", "mı",
        "ne", "nasıl", "kaç", "nerede", "hangi", "göster", "listele", "getir",
        "bazlı", "bazında", "göre", "toplam", "ortalama", "son", "tüm", "bana"
    };

    /// <summary>
    /// Kullanıcı girdisini analiz eder, bilinen terimlerle eşleşmeyenleri aday olarak kaydeder.
    /// </summary>
    public async Task<ServiceResult<int>> DiscoverFromInputAsync(
        string userInput, string? skillId = null)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return ServiceResult<int>.Success(0);

        // Kelime tokenizasyonu (basit)
        var words = userInput
            .Split(new[] { ' ', ',', '.', '?', '!', ':', ';', '(', ')' },
                   StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 2 && !StopWords.Contains(w))
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .ToList();

        if (words.Count == 0) return ServiceResult<int>.Success(0);

        // Bilinen terimleri yükle
        var knownResult = await semanticSvc.ListAsync();
        var knownTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (knownResult.IsSuccess && knownResult.Data is not null)
        {
            foreach (var def in knownResult.Data)
            {
                knownTerms.Add(def.BusinessName);
                if (!string.IsNullOrEmpty(def.Aliases))
                {
                    foreach (var alias in def.Aliases.Split(',', StringSplitOptions.TrimEntries))
                        knownTerms.Add(alias);
                }
            }
        }

        // Bilinmeyen terimleri filtrele
        var unknowns = words.Where(w => !knownTerms.Contains(w)).ToList();
        if (unknowns.Count == 0) return ServiceResult<int>.Success(0);

        return await ExecuteServiceAsync<int>(async conn =>
        {
            var added = 0;
            foreach (var term in unknowns)
            {
                // Zaten aday olarak var mı? Frekansı artır
                var existing = await conn.ExecuteScalarAsync<int?>(
                    "SELECT Id FROM SemanticLearningCandidates WHERE Term = @Term AND Status = 'Pending'",
                    new { Term = term });

                if (existing.HasValue)
                {
                    await conn.ExecuteAsync(
                        "UPDATE SemanticLearningCandidates SET Frequency = Frequency + 1 WHERE Id = @Id",
                        new { Id = existing.Value });
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO SemanticLearningCandidates
                            (Term, DetectedInSkillId, DetectedInInput, Frequency, Status, CreatedAt)
                        VALUES (@Term, @SkillId, @Input, 1, 'Pending', GETUTCDATE())",
                        new { Term = term, SkillId = skillId,
                              Input = Truncate(userInput, 500) });
                    added++;
                }
            }

            if (added > 0)
                logger.LogDebug("SemanticDiscovery: {Added} new candidates from input", added);
            return added;
        });
    }

    /// <summary>Bekleyen adayları listeler (admin review için).</summary>
    public async Task<ServiceResult<List<SemanticLearningCandidate>>> ListPendingAsync()
    {
        return await ExecuteServiceAsync<List<SemanticLearningCandidate>>(async conn =>
        {
            var rows = await conn.QueryAsync<SemanticLearningCandidate>(@"
                SELECT Id, Term, DetectedInSkillId, DetectedInInput, Frequency,
                       ProposedDefinition, Status, ReviewedBy, ReviewedAt, CreatedAt
                FROM SemanticLearningCandidates
                WHERE Status = 'Pending'
                ORDER BY Frequency DESC, CreatedAt DESC");
            return rows.ToList();
        });
    }

    /// <summary>Adayı onayla/reddet.</summary>
    public async Task<ServiceResult> ReviewCandidateAsync(int candidateId, string status, int reviewerId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(@"
                UPDATE SemanticLearningCandidates SET
                    Status = @Status, ReviewedBy = @ReviewerId,
                    ReviewedAt = GETUTCDATE()
                WHERE Id = @Id",
                new { Status = status, ReviewerId = reviewerId, Id = candidateId });
        });
    }

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..maxLen];
}
