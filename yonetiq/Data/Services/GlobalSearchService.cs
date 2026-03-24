using Dapper;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Görevler, toplantılar, kararlar, notlar, raporlar ve kullanıcılar üzerinde
/// tek bir arama terimi ile çapraz arama yapan servis.
/// </summary>
public class GlobalSearchService(IConfiguration config, AuditService? auditService)
    : BaseService(config, auditService)
{
    public async Task<ServiceResult<List<SearchResult>>> SearchAsync(string query, int maxPerType = 5)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return ServiceResult<List<SearchResult>>.Failure("Arama terimi en az 2 karakter olmalıdır.");

        return await ExecuteServiceAsync<List<SearchResult>>(async conn =>
        {
            var results = new List<SearchResult>();
            var param = new { term = $"%{query}%", max = maxPerType };

            // Görevler
            var tasks = await conn.QueryAsync<SearchResult>(@"
                SELECT TOP (@max) Id, Title AS Title, Description AS Subtitle, 'task' AS Category,
                       '/gorev/duzenle/' + CAST(Id AS NVARCHAR) AS Url, 'bi-check2-square' AS Icon
                FROM Tasks WHERE Title LIKE @term OR Description LIKE @term
                ORDER BY CreatedAt DESC", param);
            results.AddRange(tasks);

            // Toplantılar
            var meetings = await conn.QueryAsync<SearchResult>(@"
                SELECT TOP (@max) Id, Title AS Title, Location AS Subtitle, 'meeting' AS Category,
                       '/toplanti/duzenle/' + CAST(Id AS NVARCHAR) AS Url, 'bi-people' AS Icon
                FROM Meetings WHERE Title LIKE @term OR Agenda LIKE @term OR Participants LIKE @term
                ORDER BY MeetingDate DESC", param);
            results.AddRange(meetings);

            // Kararlar
            var decisions = await conn.QueryAsync<SearchResult>(@"
                SELECT TOP (@max) d.Id, d.Content AS Title, m.Title AS Subtitle, 'decision' AS Category,
                       '/toplanti/duzenle/' + CAST(d.MeetingId AS NVARCHAR) AS Url, 'bi-chat-left-dots' AS Icon
                FROM Decisions d LEFT JOIN Meetings m ON d.MeetingId = m.Id
                WHERE d.Content LIKE @term
                ORDER BY d.CreatedAt DESC", param);
            results.AddRange(decisions);

            // Notlar
            var notes = await conn.QueryAsync<SearchResult>(@"
                SELECT TOP (@max) Id, Title AS Title, Tags AS Subtitle, 'note' AS Category,
                       '/notlarim' AS Url, 'bi-journal-text' AS Icon
                FROM PersonalNotes WHERE Title LIKE @term OR Content LIKE @term OR Tags LIKE @term
                ORDER BY CreatedAt DESC", param);
            results.AddRange(notes);

            // Raporlar / Sorgular
            var queries = await conn.QueryAsync<SearchResult>(@"
                SELECT TOP (@max) Id, Name AS Title, Description AS Subtitle, 'report' AS Category,
                       '/raporlar/calistir/' + CAST(Id AS NVARCHAR) AS Url, 'bi-bar-chart-line' AS Icon
                FROM QueryRecords WHERE Name LIKE @term OR Description LIKE @term
                ORDER BY CreatedAt DESC", param);
            results.AddRange(queries);

            // Kullanıcılar
            var users = await conn.QueryAsync<SearchResult>(@"
                SELECT TOP (@max) Id, FullName AS Title, Email AS Subtitle, 'user' AS Category,
                       '/kullanicilar' AS Url, 'bi-person' AS Icon
                FROM Users WHERE FullName LIKE @term OR Email LIKE @term
                ORDER BY FullName", param);
            results.AddRange(users);

            return results;
        });
    }
}

public class SearchResult
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-search";

    public string CategoryLabel => Category switch
    {
        "task" => "Görev",
        "meeting" => "Toplantı",
        "decision" => "Karar",
        "note" => "Not",
        "report" => "Rapor",
        "user" => "Kullanıcı",
        _ => Category
    };
}
