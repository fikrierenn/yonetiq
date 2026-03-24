using Dapper;
using Microsoft.Data.SqlClient;
using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;

// Lookup grup/değer sabitleri için Infrastructure.LookupConstants kullanılır.

namespace YonetIQ.Data.Services;

/// <summary>
/// Toplantı yönetimi, karar alma ve kararların göreve dönüştürülmesi işlemlerini yöneten servis sınıfıdır.
/// </summary>
public class MeetingService(IConfiguration config, AuditService auditService) : BaseService(config, auditService)
{
    /// <summary>
    /// Sistemdeki tüm toplantı kayıtlarını (ters tarih sırasıyla) listeler.
    /// </summary>
    public async Task<ServiceResult<List<Meeting>>> GetAllAsync()
    {
        return await ExecuteServiceAsync<List<Meeting>>(async conn =>
        {
            return (await conn.QueryAsync<Meeting>(@"
                SELECT Id, Title, MeetingDate, Location, MeetingLink, Participants, Agenda, Notes,
                       RecurrenceType, RecurrenceInterval, RecurrenceEndDate, ParentMeetingId, CreatedAt
                FROM Meetings
                ORDER BY MeetingDate DESC")).ToList();
        });
    }

    /// <summary>
    /// Kimlik numarası verilen tek bir toplantı kaydını, içerisindeki Kararlar (Decisions) ile birlikte getirir.
    /// </summary>
    public async Task<ServiceResult<Meeting?>> GetByIdAsync(int id)
    {
        return await ExecuteServiceAsync<Meeting?>(async conn =>
        {
            var meeting = await conn.QueryFirstOrDefaultAsync<Meeting>(
                "SELECT * FROM Meetings WHERE Id = @Id", new { Id = id });

            if (meeting != null)
            {
                var decisions = await conn.QueryAsync<Decision>(
                    "SELECT * FROM Decisions WHERE MeetingId = @Id", new { Id = id });
                meeting.Decisions = decisions.ToList();
            }
            return meeting;
        });
    }

    /// <summary>
    /// Yeni bir toplantı kaydı oluşturur veya mevcut olanı günceller.
    /// Yeni toplantı tekrarlıysa (RecurrenceType != "None") alt örnekleri de oluşturur.
    /// </summary>
    public async Task<ServiceResult<int>> SaveAsync(Meeting meeting)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            if (meeting.Id == 0)
            {
                var sql = @"
                    INSERT INTO Meetings (Title, MeetingDate, Location, MeetingLink, Participants, Agenda, Notes,
                        RecurrenceType, RecurrenceInterval, RecurrenceEndDate, ParentMeetingId, CreatedAt)
                    OUTPUT INSERTED.Id
                    VALUES (@Title, @MeetingDate, @Location, @MeetingLink, @Participants, @Agenda, @Notes,
                        @RecurrenceType, @RecurrenceInterval, @RecurrenceEndDate, @ParentMeetingId, @CreatedAt)";
                meeting.Id = await conn.ExecuteScalarAsync<int>(sql, meeting);
                LogAction("Meeting", "Add", new { MeetingId = meeting.Id, Title = meeting.Title });

                // Tekrarlı toplantı örnekleri oluştur
                if (meeting.RecurrenceType != "None" && meeting.RecurrenceEndDate.HasValue)
                {
                    var instances = BuildRecurrenceInstances(meeting);
                    foreach (var instance in instances)
                    {
                        await conn.ExecuteAsync(sql, instance);
                    }
                }

                return meeting.Id;
            }
            else
            {
                var sql = @"
                    UPDATE Meetings
                    SET Title = @Title,
                        MeetingDate = @MeetingDate,
                        Location = @Location,
                        MeetingLink = @MeetingLink,
                        Participants = @Participants,
                        Agenda = @Agenda,
                        Notes = @Notes,
                        RecurrenceType = @RecurrenceType,
                        RecurrenceInterval = @RecurrenceInterval,
                        RecurrenceEndDate = @RecurrenceEndDate
                    WHERE Id = @Id";
                await conn.ExecuteAsync(sql, meeting);
                LogAction("Meeting", "Update", new { MeetingId = meeting.Id, Title = meeting.Title });
                return meeting.Id;
            }
        });
    }

    /// <summary>
    /// Bir toplantının seriye ait tüm gelecek örneklerini siler ve yeni örnekler oluşturur.
    /// </summary>
    public async Task<ServiceResult> DeleteRecurringSeriesAsync(int parentMeetingId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "DELETE FROM Meetings WHERE ParentMeetingId = @Id", new { Id = parentMeetingId });
            LogAction("Meeting", "DeleteSeries", new { ParentMeetingId = parentMeetingId });
        });
    }

    private static List<Meeting> BuildRecurrenceInstances(Meeting parent)
    {
        var instances = new List<Meeting>();
        var current = parent.MeetingDate;
        var endDate = parent.RecurrenceEndDate!.Value;
        var interval = parent.RecurrenceInterval < 1 ? 1 : parent.RecurrenceInterval;

        while (true)
        {
            current = parent.RecurrenceType switch
            {
                "Daily"   => current.AddDays(interval),
                "Weekly"  => current.AddDays(7 * interval),
                "Monthly" => current.AddMonths(interval),
                "Yearly"  => current.AddYears(interval),
                _         => endDate.AddDays(1) // bilinmeyen tip → döngüden çık
            };

            if (current > endDate) break;

            instances.Add(new Meeting
            {
                Title = parent.Title,
                MeetingDate = current,
                Location = parent.Location,
                MeetingLink = parent.MeetingLink,
                Participants = parent.Participants,
                Agenda = parent.Agenda,
                Notes = string.Empty,
                RecurrenceType = parent.RecurrenceType,
                RecurrenceInterval = interval,
                RecurrenceEndDate = endDate,
                ParentMeetingId = parent.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        return instances;
    }

    /// <summary>
    /// Belirtilen toplantı kaydını ve ona bağlı tüm kararları siler.
    /// </summary>
    public async Task<ServiceResult> DeleteAsync(int id)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync("DELETE FROM Decisions WHERE MeetingId = @Id", new { Id = id });
            await conn.ExecuteAsync("DELETE FROM Meetings WHERE Id = @Id", new { Id = id });
            LogAction("Meeting", "Delete", new { MeetingId = id });
        });
    }

    #region Decision Operations

    /// <summary>
    /// Bir toplantı dahilinde yeni bir karar oluşturur veya mevcut kararı günceller.
    /// </summary>
    public async Task<ServiceResult<int>> SaveDecisionAsync(Decision decision)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            if (decision.Id == 0)
            {
                var sql = @"
                    INSERT INTO Decisions (MeetingId, Content, AssigneeName, AssigneeId, DueDate, StatusLookupId, PriorityLookupId, IsConvertedToTask, LinkedTaskId, CreatedAt)
                    OUTPUT INSERTED.Id
                    VALUES (@MeetingId, @Content, @AssigneeName, @AssigneeId, @DueDate, @StatusLookupId, @PriorityLookupId, @IsConvertedToTask, @LinkedTaskId, @CreatedAt)";
                decision.Id = await conn.ExecuteScalarAsync<int>(sql, decision);
                LogAction("Decision", "Add", new { DecisionId = decision.Id, MeetingId = decision.MeetingId });
                return decision.Id;
            }
            else
            {
                var sql = @"
                    UPDATE Decisions
                    SET Content = @Content,
                        AssigneeName = @AssigneeName,
                        AssigneeId = @AssigneeId,
                        DueDate = @DueDate,
                        StatusLookupId = @StatusLookupId,
                        PriorityLookupId = @PriorityLookupId,
                        IsConvertedToTask = @IsConvertedToTask,
                        LinkedTaskId = @LinkedTaskId
                    WHERE Id = @Id";
                await conn.ExecuteAsync(sql, decision);
                LogAction("Decision", "Update", new { DecisionId = decision.Id });
                return decision.Id;
            }
        });
    }

    /// <summary>
    /// Bir kararı sistemden tamamen siler.
    /// </summary>
    public async Task<ServiceResult> DeleteDecisionAsync(int id)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync("DELETE FROM Decisions WHERE Id = @Id", new { Id = id });
            LogAction("Decision", "Delete", new { DecisionId = id });
        });
    }

    /// <summary>
    /// Bir kararın durumunu (LookupId üzerinden) günceller.
    /// </summary>
    public async Task<ServiceResult> UpdateDecisionStatusAsync(int id, int statusLookupId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "UPDATE Decisions SET StatusLookupId = @Status WHERE Id = @Id",
                new { Id = id, Status = statusLookupId });
        });
    }

    /// <summary>
    /// Bir kararı ID üzerinden getirir.
    /// </summary>
    public async Task<ServiceResult<Decision?>> GetDecisionByIdAsync(int id)
    {
        return await ExecuteServiceAsync<Decision?>(async conn =>
        {
            return await conn.QueryFirstOrDefaultAsync<Decision>(
                "SELECT * FROM Decisions WHERE Id = @Id", new { Id = id });
        });
    }

    /// <summary>
    /// Bir kararın hangi toplantıya (Meeting) ait olduğunu bulur.
    /// 
    /// DİKKAT:
    /// - Bu metot artık doğrudan int? yerine ServiceResult<int?> döner.
    /// - Böylece çağıran taraf hem başarılı/başarısız bilgisini hem de kullanıcıya gösterilecek mesajı standart şekilde yönetebilir.
    /// - Kod tabanında tüm servis metodları için ortak sonuç yapısı kullanılması hedeflendiği için bu dönüş tipi tercih edilmiştir.
    /// </summary>
    public async Task<ServiceResult<int?>> GetMeetingIdByDecisionIdAsync(int decisionId)
    {
        return await ExecuteServiceAsync<int?>(async conn =>
        {
            // İlgili karar kaydının bağlı olduğu toplantının kimlik bilgisini getirir.
            // Karar bulunamazsa null döner; bu durum da ServiceResult içinde Data = null şeklinde taşınır.
            return await conn.ExecuteScalarAsync<int?>(
                "SELECT MeetingId FROM Decisions WHERE Id = @Id", new { Id = decisionId });
        });
    }

    /// <summary>
    /// Alınan bir kararı otomatik olarak operasyonel bir göreve (TaskItem) dönüştürür.
    /// İş kuralı hataları (karar yok, zaten dönüştürülmüş vb.) ServiceResult ile;
    /// beklenmeyen teknik hatalar ise standart hata mesajları ile bildirilir.
    /// </summary>
    public async Task<ServiceResult<int>> ConvertDecisionToTaskAsync(int decisionId)
    {
        try
        {
            await using var conn = CreateConn();
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            // 1. Karar detaylarını getir
            var decision = await conn.QueryFirstOrDefaultAsync<Decision>(
                "SELECT * FROM Decisions WHERE Id = @Id", new { Id = decisionId }, transaction);

            if (decision is null)
            {
                return ServiceResult<int>.Failure("Karar bulunamadı.", "DecisionNotFound");
            }

            if (decision.IsConvertedToTask)
            {
                return ServiceResult<int>.Failure("Bu karar zaten bir göreve dönüştürülmüş.", "DecisionAlreadyConverted");
            }

            // 2. Lookup ID'lerini getir (dinamik eşleme için)
            var lookups = (await conn.QueryAsync<(int Id, string Group, string Value)>(
                "SELECT Id, [Group], Value FROM Lookups WHERE [Group] IN ('" + LookupConstants.GroupTaskStatus + "', '" + LookupConstants.GroupTaskPriority + "', '" + LookupConstants.GroupDecisionStatus + "')",
                null, transaction)).ToList();

            int GetLookupId(string group, string value) =>
                lookups.FirstOrDefault(l => l.Group == group && l.Value == value).Id;

            // 2.1 Görev Önceliği: Karardan al, yoksa Normal ata
            var taskPriorityId = decision.PriorityLookupId ?? GetLookupId(LookupConstants.GroupTaskPriority, LookupConstants.ValueNormal);

            // 2.2 Görev Durumu: Karardan al, yoksa Bekliyor ata. Karar Tamamlandi ise görev de Tamamlandi başlar.
            var completedDecisionId = GetLookupId(LookupConstants.GroupDecisionStatus, LookupConstants.ValueTamamlandi);
            var taskStatusId = decision.StatusLookupId == completedDecisionId
                ? GetLookupId(LookupConstants.GroupTaskStatus, LookupConstants.ValueTamamlandi)
                : GetLookupId(LookupConstants.GroupTaskStatus, LookupConstants.ValueBekliyor);

            // 3. Yeni görev (TaskItem) oluştur
            var taskSql = @"
                INSERT INTO TaskItems (Title, Description, AssigneeName, AssigneeId, AssignedAt, DueDate, PriorityLookupId, StatusLookupId, SourceEntityType, SourceEntityId, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES (@Title, @Description, @AssigneeName, @AssigneeId, @AssignedAt, @DueDate, @PriorityLookupId, @StatusLookupId, @SourceEntityType, @SourceEntityId, @CreatedAt)";

            var taskId = await conn.ExecuteScalarAsync<int>(taskSql, new
            {
                Title = $"Toplantı Kararı: {(decision.Content.Length > 50 ? decision.Content[..47] + "..." : decision.Content)}",
                Description = decision.Content,
                decision.AssigneeName,
                decision.AssigneeId,
                AssignedAt = DateTime.UtcNow,
                decision.DueDate,
                PriorityLookupId = taskPriorityId,
                StatusLookupId = taskStatusId,
                SourceEntityType = "Decision",
                SourceEntityId = decisionId,
                CreatedAt = DateTime.UtcNow
            }, transaction);

            // 4. Kararı güncelle
            var updateSql = @"
                UPDATE Decisions 
                SET IsConvertedToTask = 1, LinkedTaskId = @TaskId 
                WHERE Id = @Id";

            await conn.ExecuteAsync(updateSql, new { TaskId = taskId, Id = decisionId }, transaction);

            transaction.Commit();
            LogAction("Decision", "ConvertToTask", new { DecisionId = decisionId, TaskId = taskId });
            return ServiceResult<int>.Success(taskId, "Karar başarıyla göreve dönüştürüldü.");
        }
        catch (SqlException ex)
        {
            return ServiceResult<int>.Failure("Kararı göreve dönüştürürken veritabanı hatası oluştu.", ex.Number.ToString());
        }
        catch (Exception ex)
        {
            return ServiceResult<int>.Failure("Kararı göreve dönüştürürken sistem hatası oluştu.", ex.Message);
        }
    }

    /// <summary>Bugünkü toplantıları döner</summary>
    public async Task<ServiceResult<List<Meeting>>> GetTodayMeetingsAsync()
    {
        return await ExecuteServiceAsync<List<Meeting>>(async conn =>
        {
            var result = await conn.QueryAsync<Meeting>(@"
                SELECT Id, Title, MeetingDate, Location, MeetingLink, Participants
                FROM Meetings
                WHERE CAST(MeetingDate AS DATE) = CAST(GETUTCDATE() AS DATE)
                ORDER BY MeetingDate",
                commandTimeout: 30);
            return result.ToList();
        });
    }

    /// <summary>
    /// Henüz tamamlanmamış karar sayısını döner.
    /// </summary>
    public async Task<ServiceResult<int>> GetPendingDecisionCountAsync()
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            return await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM Decisions
                WHERE StatusLookupId <> (
                    SELECT Id FROM Lookups WHERE [Group] = '" + LookupConstants.GroupDecisionStatus + @"' AND Value = '" + LookupConstants.ValueTamamlandi + @"'
                )");
        });
    }

    /// <summary>
    /// En son alınan kararları listeler.
    /// </summary>
    public async Task<ServiceResult<List<Decision>>> ListLatestDecisionsAsync(int count = 10)
    {
        return await ExecuteServiceAsync<List<Decision>>(async conn =>
        {
            var sql = $@"
                SELECT TOP {count} d.*, m.Title as MeetingTitle
                FROM Decisions d
                JOIN Meetings m ON d.MeetingId = m.Id
                ORDER BY d.CreatedAt DESC";
            return (await conn.QueryAsync<Decision>(sql)).ToList();
        });
    }

    /// <summary>
    /// Belirtilen toplantıya ait tüm kararları listeler.
    /// </summary>
    public async Task<ServiceResult<List<Decision>>> ListDecisionsAsync(int meetingId)
    {
        return await ExecuteServiceAsync<List<Decision>>(async conn =>
        {
            var sql = "SELECT * FROM Decisions WHERE MeetingId = @MeetingId ORDER BY CreatedAt DESC";
            return (await conn.QueryAsync<Decision>(sql, new { MeetingId = meetingId })).ToList();
        });
    }

    /// <summary>
    /// Kararların göreve dönüştürülme yoğunluğunu (yüzdesini) döner.
    /// </summary>
    public async Task<ServiceResult<int>> GetDecisionDensityAsync()
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            var sql = @"
                SELECT 
                    CASE 
                        WHEN COUNT(*) = 0 THEN 0 
                        ELSE CAST(100.0 * SUM(CASE WHEN IsConvertedToTask = 1 THEN 1 ELSE 0 END) / COUNT(*) AS INT) 
                    END
                FROM Decisions";
            return await conn.ExecuteScalarAsync<int>(sql);
        });
    }

    #endregion
}
