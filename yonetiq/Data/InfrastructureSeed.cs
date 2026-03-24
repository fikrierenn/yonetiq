using Dapper;
using Microsoft.Data.SqlClient;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data;

/// <summary>
/// Veritabanı şeması ve altyapı hazırlık işlemlerini yöneten partial sınıf.
/// </summary>
public static partial class SeedData
{
    private static async Task PrepareOkrInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Objectives')
        BEGIN
            CREATE TABLE Objectives (
                Id          INT IDENTITY(1,1) PRIMARY KEY,
                Title       NVARCHAR(300) NOT NULL,
                Description NVARCHAR(1000) NULL,
                OwnerId     INT NULL,
                OwnerName   NVARCHAR(150) NOT NULL DEFAULT '',
                Level       NVARCHAR(20) NOT NULL DEFAULT 'Company',
                Period      NVARCHAR(20) NOT NULL DEFAULT 'Q1',
                Year        INT NOT NULL DEFAULT 2026,
                Status      NVARCHAR(20) NOT NULL DEFAULT 'Active',
                Progress    DECIMAL(5,2) NOT NULL DEFAULT 0,
                CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
        END
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'KeyResults')
        BEGIN
            CREATE TABLE KeyResults (
                Id              INT IDENTITY(1,1) PRIMARY KEY,
                ObjectiveId     INT NOT NULL,
                Title           NVARCHAR(300) NOT NULL,
                MetricType      NVARCHAR(20) NOT NULL DEFAULT 'Number',
                StartValue      DECIMAL(18,2) NOT NULL DEFAULT 0,
                TargetValue     DECIMAL(18,2) NOT NULL,
                CurrentValue    DECIMAL(18,2) NOT NULL DEFAULT 0,
                Unit            NVARCHAR(50) NULL,
                OwnerId         INT NULL,
                OwnerName       NVARCHAR(150) NOT NULL DEFAULT '',
                LinkedTaskId    INT NULL,
                Progress        DECIMAL(5,2) NOT NULL DEFAULT 0,
                CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT FK_KeyResults_Objective FOREIGN KEY (ObjectiveId) REFERENCES Objectives(Id) ON DELETE CASCADE
            );
            CREATE INDEX IX_KeyResults_ObjectiveId ON KeyResults (ObjectiveId);
        END
        ");
    }

    private static async Task PrepareUserPasswordColumnsAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.Users', 'PasswordHash') IS NULL
BEGIN
    ALTER TABLE Users ADD PasswordHash NVARCHAR(500) NULL;
    ALTER TABLE Users ADD PasswordSalt NVARCHAR(500) NULL;
END");
    }

    private static async Task PrepareLookupsTableAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Lookups')
BEGIN
    CREATE TABLE Lookups
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Group] NVARCHAR(100) NOT NULL,
        Value NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Lookups_IsActive DEFAULT(1),
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_Lookups_Group ON Lookups([Group]);
END");

        await StoredProcedureLoader.LoadAndExecuteAsync(conn, "sp_Lookup_ListByGroup");
    }

    private static async Task PrepareUserLookupColumnsAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.Users', 'RoleLookupId') IS NULL
BEGIN
    ALTER TABLE Users ADD RoleLookupId INT NULL;
    ALTER TABLE Users ADD CONSTRAINT FK_User_Role FOREIGN KEY (RoleLookupId) REFERENCES Lookups(Id);
END");

        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.Users', 'DepartmentLookupId') IS NULL
BEGIN
    ALTER TABLE Users ADD DepartmentLookupId INT NULL;
    ALTER TABLE Users ADD CONSTRAINT FK_User_Department FOREIGN KEY (DepartmentLookupId) REFERENCES Lookups(Id);
END");
    }

    private static async Task PrepareTaskAndDecisionLookupColumnsAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.TaskItems', 'PriorityLookupId') IS NULL
BEGIN
    ALTER TABLE TaskItems ADD PriorityLookupId INT NULL;
    ALTER TABLE TaskItems ADD CONSTRAINT FK_Task_Priority FOREIGN KEY (PriorityLookupId) REFERENCES Lookups(Id);
    
    ALTER TABLE TaskItems ADD StatusLookupId INT NULL;
    ALTER TABLE TaskItems ADD CONSTRAINT FK_Task_Status FOREIGN KEY (StatusLookupId) REFERENCES Lookups(Id);
END");

        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.Decisions', 'StatusLookupId') IS NULL
BEGIN
    ALTER TABLE Decisions ADD StatusLookupId INT NULL;
    ALTER TABLE Decisions ADD CONSTRAINT FK_Decision_Status FOREIGN KEY (StatusLookupId) REFERENCES Lookups(Id);

    ALTER TABLE Decisions ADD PriorityLookupId INT NULL;
    ALTER TABLE Decisions ADD CONSTRAINT FK_Decision_Priority FOREIGN KEY (PriorityLookupId) REFERENCES Lookups(Id);
END");
    }

    private static async Task PrepareCalendarSpecialDayInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CalendarSpecialDays')
BEGIN
    CREATE TABLE CalendarSpecialDays
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Date DATE NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        TypeLookupId INT NOT NULL,
        Detail NVARCHAR(1000) NULL,
        IsAnnualRecurring BIT NOT NULL DEFAULT(0),
        IsActive BIT NOT NULL DEFAULT(1),
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())
    );
    CREATE INDEX IX_CalendarDays_Date ON CalendarSpecialDays(Date);
END");

        await StoredProcedureLoader.LoadAndExecuteAsync(conn, "sp_CalendarSpecialDay_List");
        await StoredProcedureLoader.LoadAndExecuteAsync(conn, "sp_CalendarSpecialDay_ListAsEvents");
        await StoredProcedureLoader.LoadAndExecuteAsync(conn, "sp_Calendar_ListAsEvents");
        await StoredProcedureLoader.LoadAndExecuteAsync(conn, "sp_CalendarSpecialDay_Save");
        await StoredProcedureLoader.LoadAndExecuteAsync(conn, "sp_CalendarSpecialDay_Delete");
    }

    private static async Task PrepareCalendarSpecialDayLookupColumnsAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = 'FK_Calendar_Type')
BEGIN
    ALTER TABLE CalendarSpecialDays ADD CONSTRAINT FK_Calendar_Type FOREIGN KEY (TypeLookupId) REFERENCES Lookups(Id);
END");
    }

    private static async Task PrepareReportFavoriteInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ReportFavorites')
BEGIN
    CREATE TABLE ReportFavorites
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        QueryId INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Fav_User FOREIGN KEY (UserId) REFERENCES Users(Id),
        CONSTRAINT FK_Fav_Query FOREIGN KEY (QueryId) REFERENCES QueryRecords(Id)
    );
END");
    }

    private static async Task PrepareQueryUsagePurposeLookupColumnsAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.QueryRecords', 'UsagePurposeLookupId') IS NULL
BEGIN
    ALTER TABLE QueryRecords ADD UsagePurposeLookupId INT NULL;
    ALTER TABLE QueryRecords ADD CONSTRAINT FK_Query_Usage FOREIGN KEY (UsagePurposeLookupId) REFERENCES Lookups(Id);
END");
    }

    private static async Task PrepareSystemLogInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SystemLogs')
BEGIN
    CREATE TABLE SystemLogs
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NULL,
        Module NVARCHAR(100) NOT NULL,
        Action NVARCHAR(100) NOT NULL,
        Detail NVARCHAR(MAX) NULL,
        IpAddress NVARCHAR(50) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())
    );
    CREATE INDEX IX_SystemLogs_Module ON SystemLogs(Module);
    CREATE INDEX IX_SystemLogs_CreatedAt ON SystemLogs(CreatedAt);
END");
    }

    private static async Task PrepareReportShareInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ReportShares')
BEGIN
    CREATE TABLE ReportShares
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        QueryId INT NOT NULL,
        SharedByUserId INT NOT NULL,
        TargetUserId INT NULL,
        TargetDepartmentLookupId INT NULL,
        Note NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Share_Query FOREIGN KEY (QueryId) REFERENCES QueryRecords(Id),
        CONSTRAINT FK_Share_By FOREIGN KEY (SharedByUserId) REFERENCES Users(Id),
        CONSTRAINT FK_Share_With FOREIGN KEY (TargetUserId) REFERENCES Users(Id),
        CONSTRAINT FK_Share_Dept FOREIGN KEY (TargetDepartmentLookupId) REFERENCES Lookups(Id)
    );
END");

        // Mevcut kurulumlar için kolon adı düzeltmesi (SharedWithUserId → TargetUserId, Message → Note)
        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.ReportShares', 'SharedWithUserId') IS NOT NULL
BEGIN
    EXEC sp_rename 'ReportShares.SharedWithUserId', 'TargetUserId', 'COLUMN';
END");
        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.ReportShares', 'Message') IS NOT NULL
BEGIN
    EXEC sp_rename 'ReportShares.Message', 'Note', 'COLUMN';
END");
        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.ReportShares', 'TargetDepartmentLookupId') IS NULL
BEGIN
    ALTER TABLE ReportShares ADD TargetDepartmentLookupId INT NULL;
    ALTER TABLE ReportShares ADD CONSTRAINT FK_Share_Dept FOREIGN KEY (TargetDepartmentLookupId) REFERENCES Lookups(Id);
END");
    }

    private static async Task PrepareCommunicationMessageInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CommunicationMessages')
BEGIN
    CREATE TABLE CommunicationMessages
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SenderUserId INT NULL,
        ReceiverUserId INT NULL,
        Body NVARCHAR(MAX) NOT NULL,
        Type INT NOT NULL DEFAULT(1),
        IsRead BIT NOT NULL DEFAULT(0),
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Msg_Sender FOREIGN KEY (SenderUserId) REFERENCES Users(Id),
        CONSTRAINT FK_Msg_Receiver FOREIGN KEY (ReceiverUserId) REFERENCES Users(Id)
    );
END");
    }

    private static async Task PrepareAttachmentInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FileAttachments')
BEGIN
    CREATE TABLE FileAttachments
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FileName NVARCHAR(255) NOT NULL,
        StoragePath NVARCHAR(500) NOT NULL,
        ContentType NVARCHAR(100) NOT NULL,
        FileSize BIGINT NOT NULL,
        RelatedEntityType NVARCHAR(50) NOT NULL,
        RelatedEntityId INT NOT NULL,
        CreatedByUserId INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Attachment_Creator FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
    );
    CREATE INDEX IX_FileAttachments_Related ON FileAttachments(RelatedEntityType, RelatedEntityId);
END");
    }

    private static async Task PrepareMeetingRecurrenceColumnsAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.Meetings', 'RecurrenceType') IS NULL
BEGIN
    ALTER TABLE Meetings ADD RecurrenceType NVARCHAR(20) NOT NULL DEFAULT('None');
    ALTER TABLE Meetings ADD RecurrenceInterval INT NOT NULL DEFAULT(1);
    ALTER TABLE Meetings ADD RecurrenceEndDate DATETIME2 NULL;
    ALTER TABLE Meetings ADD ParentMeetingId INT NULL;
    ALTER TABLE Meetings ADD CONSTRAINT FK_Meeting_Parent FOREIGN KEY (ParentMeetingId) REFERENCES Meetings(Id) ON DELETE NO ACTION;
END");
    }

    private static async Task PreparePersonalNotesInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PersonalNotes')
BEGIN
    CREATE TABLE PersonalNotes
    (
        Id                  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId              INT NOT NULL,
        Title               NVARCHAR(300) NOT NULL DEFAULT(''),
        Content             NVARCHAR(MAX) NOT NULL DEFAULT(''),
        Tags                NVARCHAR(500) NOT NULL DEFAULT(''),
        ReminderAt          DATETIME2 NULL,
        IsReminderDismissed BIT NOT NULL DEFAULT(0),
        LinkedTaskId        INT NULL,
        CreatedAt           DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_PersonalNote_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_PersonalNote_Task FOREIGN KEY (LinkedTaskId) REFERENCES TaskItems(Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_PersonalNotes_User ON PersonalNotes(UserId);
    CREATE INDEX IX_PersonalNotes_Reminder ON PersonalNotes(UserId, ReminderAt) WHERE ReminderAt IS NOT NULL;
END");
    }

    /// <summary>
    /// Çoklu sunucu bağlantılarını saklayan DataSources tablosunu oluşturur.
    /// QueryRecords tablosuna DataSourceId FK kolonunu ekler.
    /// </summary>
    private static async Task PrepareDataSourcesInfrastructureAsync(SqlConnection conn)
    {
        // DataSources tablosu
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DataSources')
BEGIN
    CREATE TABLE DataSources
    (
        Id                         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name                       NVARCHAR(200) NOT NULL,
        Description                NVARCHAR(500) NULL,
        ServerType                 NVARCHAR(20)  NOT NULL DEFAULT('MSSQL'),
        EncryptedConnectionString  NVARCHAR(MAX) NOT NULL,
        IsDefault                  BIT NOT NULL DEFAULT(0),
        IsActive                   BIT NOT NULL DEFAULT(1),
        CreatedByUserId            INT NULL,
        CreatedAt                  DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        UpdatedAt                  DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())
    );
    CREATE INDEX IX_DataSources_IsDefault ON DataSources(IsDefault);
END");

        // QueryRecords.DataSourceId kolonu (migration henüz uygulanmamış ortamlar için guard)
        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.QueryRecords', 'DataSourceId') IS NULL
BEGIN
    ALTER TABLE QueryRecords ADD DataSourceId INT NULL;
    ALTER TABLE QueryRecords ADD CONSTRAINT FK_QueryRecord_DataSource
        FOREIGN KEY (DataSourceId) REFERENCES DataSources(Id) ON DELETE SET NULL;
END");

        // QueryRecords.AllowDml kolonu — DML/Uzman Modu bayrağı
        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.QueryRecords', 'AllowDml') IS NULL
BEGIN
    ALTER TABLE QueryRecords ADD AllowDml BIT NOT NULL CONSTRAINT DF_QueryRecords_AllowDml DEFAULT(0);
END");
    }

    /// <summary>
    /// Sistem ayarlarını (SMTP, Telegram vs.) şifreli saklayan SystemSettings tablosunu oluşturur.
    /// </summary>
    private static async Task PrepareSystemSettingsInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SystemSettings')
BEGIN
    CREATE TABLE SystemSettings
    (
        [Key]         NVARCHAR(100) NOT NULL PRIMARY KEY,
        [Value]       NVARCHAR(MAX) NULL,
        [IsEncrypted] BIT           NOT NULL DEFAULT(0),
        [Group]       NVARCHAR(50)  NULL,
        [Description] NVARCHAR(200) NULL,
        UpdatedAt     DATETIME2     NOT NULL DEFAULT(SYSUTCDATETIME())
    );

    INSERT INTO SystemSettings ([Key],[Value],[IsEncrypted],[Group],[Description]) VALUES
    ('Email.Host',           NULL,      0, 'Email',    'SMTP sunucusu (ör: smtp.office365.com)'),
    ('Email.Port',           '587',     0, 'Email',    'SMTP portu (587 veya 465)'),
    ('Email.UseSsl',         'true',    0, 'Email',    'SSL/TLS kullan (true/false)'),
    ('Email.Username',       NULL,      0, 'Email',    'SMTP kullanıcı adı / e-posta'),
    ('Email.Password',       NULL,      1, 'Email',    'SMTP şifresi (şifreli saklanır)'),
    ('Email.FromAddress',    NULL,      0, 'Email',    'Gönderen e-posta adresi'),
    ('Email.FromName',       'YonetIQ', 0, 'Email',    'Gönderen görünen adı'),
    ('Telegram.BotToken',    NULL,      1, 'Telegram', 'Bot token (BotFather''dan alınır, şifreli)'),
    ('Telegram.BotUsername', NULL,      0, 'Telegram', 'Bot kullanıcı adı (ör: YonetIQ_bot, Login Widget için)');
END");

        // Users tablosu — Telegram ve şifre sıfırlama kolonları
        await conn.ExecuteAsync(@"
IF COL_LENGTH('dbo.Users','TelegramChatId') IS NULL
    ALTER TABLE Users ADD TelegramChatId NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.Users','PasswordResetToken') IS NULL
    ALTER TABLE Users ADD PasswordResetToken NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.Users','PasswordResetExpiry') IS NULL
    ALTER TABLE Users ADD PasswordResetExpiry DATETIME2 NULL;");

        // Eksik Lookup gruplarını garantiye al (SeedLookupsAsync sadece ilk çalışmada tüm grubu ekler;
        // sonraki ortamlarda eklenen yeni gruplar buradan IF NOT EXISTS ile sağlanır)
        // CreatedAt açıkça belirtilir — DEFAULT constraint olmayan eski şemalarla da çalışır.
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM Lookups WHERE [Group]='ResultType')
BEGIN
    INSERT INTO Lookups([Group],[Value],[IsActive],[CreatedAt]) VALUES
    ('ResultType','Table',        1, SYSUTCDATETIME()),
    ('ResultType','PivotTable',   1, SYSUTCDATETIME()),
    ('ResultType','KpiCard',      1, SYSUTCDATETIME()),
    ('ResultType','MultiKpi',     1, SYSUTCDATETIME()),
    ('ResultType','BarChart',     1, SYSUTCDATETIME()),
    ('ResultType','LineChart',    1, SYSUTCDATETIME()),
    ('ResultType','AreaChart',    1, SYSUTCDATETIME()),
    ('ResultType','PieChart',     1, SYSUTCDATETIME()),
    ('ResultType','DonutChart',   1, SYSUTCDATETIME()),
    ('ResultType','HeatmapChart', 1, SYSUTCDATETIME()),
    ('ResultType','ScatterChart', 1, SYSUTCDATETIME()),
    ('ResultType','RadarChart',   1, SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM Lookups WHERE [Group]='SetType')
BEGIN
    INSERT INTO Lookups([Group],[Value],[IsActive],[CreatedAt]) VALUES
    ('SetType','Dashboard', 1, SYSUTCDATETIME()),
    ('SetType','KpiPanel',  1, SYSUTCDATETIME()),
    ('SetType','Report',    1, SYSUTCDATETIME()),
    ('SetType','Custom',    1, SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM Lookups WHERE [Group]='QueryUsage')
BEGIN
    INSERT INTO Lookups([Group],[Value],[IsActive],[CreatedAt]) VALUES
    ('QueryUsage','Dashboard', 1, SYSUTCDATETIME()),
    ('QueryUsage','Report',    1, SYSUTCDATETIME()),
    ('QueryUsage','Both',      1, SYSUTCDATETIME());
END");
    }

    /// <summary>
    /// Notifications tablosunu oluşturur (yoksa).
    /// </summary>
    private static async Task PrepareNotificationsInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Notifications')
BEGIN
    CREATE TABLE Notifications
    (
        Id         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId     INT NOT NULL,
        Type       NVARCHAR(50)   NOT NULL,
        Title      NVARCHAR(300)  NOT NULL,
        Message    NVARCHAR(1000) NOT NULL DEFAULT(''),
        RelatedUrl NVARCHAR(500)  NULL,
        IsRead     BIT            NOT NULL DEFAULT(0),
        CreatedAt  DATETIME2      NOT NULL DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Notifications_User_IsRead ON Notifications(UserId, IsRead);
    CREATE INDEX IX_Notifications_CreatedAt   ON Notifications(CreatedAt DESC);
END");
    }

    /// <summary>
    /// TaskComments ve TaskActivity tablolarını oluşturur (yoksa).
    /// </summary>
    private static async Task PrepareTaskCommentInfrastructureAsync(SqlConnection conn)
    {
        // TaskComments tablosu
        await conn.ExecuteAsync(@"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TaskComments')
        BEGIN
            CREATE TABLE TaskComments (
                Id          INT IDENTITY(1,1) PRIMARY KEY,
                TaskItemId  INT NOT NULL,
                UserId      INT NOT NULL,
                UserName    NVARCHAR(150) NOT NULL DEFAULT '',
                Content     NVARCHAR(2000) NOT NULL,
                CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT FK_TaskComments_Task FOREIGN KEY (TaskItemId) REFERENCES TaskItems(Id) ON DELETE CASCADE
            );
            CREATE INDEX IX_TaskComments_TaskItemId ON TaskComments (TaskItemId);
        END
    ");

        // TaskActivity tablosu
        await conn.ExecuteAsync(@"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TaskActivity')
        BEGIN
            CREATE TABLE TaskActivity (
                Id          INT IDENTITY(1,1) PRIMARY KEY,
                TaskItemId  INT NOT NULL,
                UserId      INT NOT NULL,
                UserName    NVARCHAR(150) NOT NULL DEFAULT '',
                ActionType  NVARCHAR(50) NOT NULL,
                OldValue    NVARCHAR(500) NULL,
                NewValue    NVARCHAR(500) NULL,
                Note        NVARCHAR(500) NULL,
                CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT FK_TaskActivity_Task FOREIGN KEY (TaskItemId) REFERENCES TaskItems(Id) ON DELETE CASCADE
            );
            CREATE INDEX IX_TaskActivity_TaskItemId ON TaskActivity (TaskItemId);
        END
    ");
    }

    private static async Task PrepareScheduledReportsInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ScheduledReports')
            BEGIN
                CREATE TABLE ScheduledReports (
                    Id              INT IDENTITY(1,1) PRIMARY KEY,
                    QueryRecordId   INT NOT NULL,
                    Title           NVARCHAR(200) NOT NULL,
                    Channel         NVARCHAR(20) NOT NULL DEFAULT 'Email',
                    Recipient       NVARCHAR(300) NOT NULL,
                    FrequencyType   NVARCHAR(20) NOT NULL DEFAULT 'Daily',
                    RunTime         TIME NOT NULL DEFAULT '08:00',
                    DayOfWeek       INT NULL,
                    DayOfMonth      INT NULL,
                    IsActive        BIT NOT NULL DEFAULT 1,
                    LastRunAt       DATETIME2 NULL,
                    NextRunAt       DATETIME2 NULL,
                    CreatedBy       INT NOT NULL DEFAULT 1,
                    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT FK_ScheduledReports_Query FOREIGN KEY (QueryRecordId) REFERENCES QueryRecords(Id) ON DELETE CASCADE
                );
            END
        ");
    }

    /// <summary>
    /// K1 — Tüm Türkçe karakter normalizasyonu (Genel Mudur, Direktor, Mudur, Satis vb.)
    /// Mevcut veritabanlarında Lookups ve Users tablolarındaki ASCII değerleri düzeltir.
    /// Bu yöntem idempotent'tir; doğru değer zaten varsa hiçbir şey yapmaz.
    /// </summary>
    private static async Task PrepareKpiTargetsInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'KpiTargets')
        BEGIN
            CREATE TABLE KpiTargets (
                Id          INT IDENTITY(1,1) PRIMARY KEY,
                MetricKey   NVARCHAR(100) NOT NULL,
                Label       NVARCHAR(200) NOT NULL,
                TargetValue DECIMAL(18,2) NOT NULL,
                Operator    NVARCHAR(5) NOT NULL DEFAULT '<=',
                PeriodType  NVARCHAR(20) NOT NULL DEFAULT 'AllTime',
                IsActive    BIT NOT NULL DEFAULT 1,
                CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
        END
    ");
    }

    private static async Task PrepareTaskTemplatesInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TaskTemplates')
        BEGIN
            CREATE TABLE TaskTemplates (
                Id          INT IDENTITY(1,1) PRIMARY KEY,
                Name        NVARCHAR(200) NOT NULL,
                Description NVARCHAR(1000) NULL,
                Category    NVARCHAR(100) NULL,
                IsActive    BIT NOT NULL DEFAULT 1,
                CreatedBy   INT NOT NULL DEFAULT 1,
                CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
        END
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TaskTemplateItems')
        BEGIN
            CREATE TABLE TaskTemplateItems (
                Id              INT IDENTITY(1,1) PRIMARY KEY,
                TaskTemplateId  INT NOT NULL,
                Title           NVARCHAR(300) NOT NULL,
                Description     NVARCHAR(1000) NULL,
                Priority        NVARCHAR(50) NULL,
                DueDays         INT NULL,
                OrderIndex      INT NOT NULL DEFAULT 0,
                CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT FK_TaskTemplateItems_Template FOREIGN KEY (TaskTemplateId) REFERENCES TaskTemplates(Id) ON DELETE CASCADE
            );
        END
    ");
    }

    private static async Task PrepareTimeTrackingInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TimeEntries')
        BEGIN
            CREATE TABLE TimeEntries (
                Id          INT IDENTITY(1,1) PRIMARY KEY,
                TaskItemId  INT NOT NULL,
                UserId      INT NOT NULL,
                UserName    NVARCHAR(150) NOT NULL DEFAULT '',
                StartedAt   DATETIME2 NOT NULL,
                EndedAt     DATETIME2 NULL,
                DurationMin INT NULL,
                Note        NVARCHAR(500) NULL,
                CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT FK_TimeEntries_Task FOREIGN KEY (TaskItemId) REFERENCES TaskItems(Id) ON DELETE CASCADE
            );
            CREATE INDEX IX_TimeEntries_TaskItemId ON TimeEntries (TaskItemId);
            CREATE INDEX IX_TimeEntries_UserId ON TimeEntries (UserId);
        END
    ");
    }

    private static async Task PrepareApprovalInfrastructureAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ApprovalRequests')
        BEGIN
            CREATE TABLE ApprovalRequests (
                Id              INT IDENTITY(1,1) PRIMARY KEY,
                EntityType      NVARCHAR(50) NOT NULL,
                EntityId        INT NULL,
                Title           NVARCHAR(300) NOT NULL,
                Description     NVARCHAR(1000) NULL,
                RequestedBy     INT NOT NULL,
                RequestedByName NVARCHAR(150) NOT NULL DEFAULT '',
                Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending',
                CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
        END
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ApprovalSteps')
        BEGIN
            CREATE TABLE ApprovalSteps (
                Id                INT IDENTITY(1,1) PRIMARY KEY,
                ApprovalRequestId INT NOT NULL,
                StepOrder         INT NOT NULL DEFAULT 1,
                ApproverId        INT NOT NULL,
                ApproverName      NVARCHAR(150) NOT NULL DEFAULT '',
                Status            NVARCHAR(20) NOT NULL DEFAULT 'Pending',
                Comment           NVARCHAR(500) NULL,
                ActionAt          DATETIME2 NULL,
                CreatedAt         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT FK_ApprovalSteps_Request FOREIGN KEY (ApprovalRequestId)
                    REFERENCES ApprovalRequests(Id) ON DELETE CASCADE
            );
            CREATE INDEX IX_ApprovalSteps_RequestId  ON ApprovalSteps (ApprovalRequestId);
            CREATE INDEX IX_ApprovalSteps_ApproverId ON ApprovalSteps (ApproverId);
        END
    ");
    }

    private static async Task NormalizeRoleValuesAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
-- Rol lookup değerlerini Türkçe'ye normalize et
UPDATE Lookups SET Value = N'Genel Müdür'  WHERE [Group]='Role'       AND Value='Genel Mudur';
UPDATE Lookups SET Value = N'Direktör'     WHERE [Group]='Role'       AND Value='Direktor';
UPDATE Lookups SET Value = N'Müdür'        WHERE [Group]='Role'       AND Value='Mudur';

-- Departman lookup değerlerini Türkçe'ye normalize et
UPDATE Lookups SET Value = N'Yönetim'         WHERE [Group]='Department' AND Value='Yonetim';
UPDATE Lookups SET Value = N'Satış'           WHERE [Group]='Department' AND Value='Satis';
UPDATE Lookups SET Value = N'İnsan Kaynakları' WHERE [Group]='Department' AND Value='Insan Kaynaklari';

-- Kullanıcı FullName alanlarını Türkçe'ye normalize et
UPDATE Users SET FullName = N'Genel Müdür'      WHERE FullName = 'Genel Mudur';
UPDATE Users SET FullName = N'Finans Direktörü' WHERE FullName = 'Finans Direktoru';
UPDATE Users SET FullName = N'Satış Direktörü'  WHERE FullName = 'Satis Direktoru';
UPDATE Users SET FullName = N'IT Müdürü'        WHERE FullName = 'IT Muduru';
UPDATE Users SET FullName = N'Operasyon Müdürü' WHERE FullName = 'Operasyon Muduru';");
    }

    private static async Task PrepareAiInfrastructureAsync(SqlConnection conn)
    {
        // AiInteractions table
        await conn.ExecuteAsync(@"
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AiInteractions')
    BEGIN
        CREATE TABLE AiInteractions (
            Id              INT IDENTITY(1,1) PRIMARY KEY,
            UserId          INT NOT NULL,
            SkillId         NVARCHAR(100) NOT NULL,
            Module          NVARCHAR(50) NOT NULL DEFAULT '',
            InputSummary    NVARCHAR(500) NOT NULL DEFAULT '',
            OutputSummary   NVARCHAR(500) NOT NULL DEFAULT '',
            ConfidenceScore DECIMAL(3,2) NOT NULL DEFAULT 0,
            LatencyMs       INT NOT NULL DEFAULT 0,
            TokenCount      INT NOT NULL DEFAULT 0,
            IsSuccess       BIT NOT NULL DEFAULT 1,
            CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
        );
        CREATE INDEX IX_AiInteractions_UserId ON AiInteractions (UserId);
        CREATE INDEX IX_AiInteractions_SkillId ON AiInteractions (SkillId);
        CREATE INDEX IX_AiInteractions_CreatedAt ON AiInteractions (CreatedAt DESC);
    END
    ");

        // AiFeedback table
        await conn.ExecuteAsync(@"
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AiFeedback')
    BEGIN
        CREATE TABLE AiFeedback (
            Id              INT IDENTITY(1,1) PRIMARY KEY,
            InteractionId   INT NOT NULL,
            UserId          INT NOT NULL,
            Rating          TINYINT NOT NULL DEFAULT 0,
            CorrectionText  NVARCHAR(MAX) NULL,
            WasAccepted     BIT NOT NULL DEFAULT 0,
            WasEdited       BIT NOT NULL DEFAULT 0,
            CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
            CONSTRAINT FK_AiFeedback_Interaction FOREIGN KEY (InteractionId) REFERENCES AiInteractions(Id) ON DELETE CASCADE
        );
        CREATE INDEX IX_AiFeedback_InteractionId ON AiFeedback (InteractionId);
    END
    ");
    }

    private static async Task PrepareSemanticDefinitionsAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SemanticDefinitions')
    BEGIN
        CREATE TABLE SemanticDefinitions (
            Id              INT IDENTITY(1,1) PRIMARY KEY,
            TermType        NVARCHAR(20) NOT NULL,
            TableName       NVARCHAR(128) NULL,
            ColumnName      NVARCHAR(128) NULL,
            BusinessName    NVARCHAR(200) NOT NULL,
            Description     NVARCHAR(500) NULL,
            SqlExpression   NVARCHAR(500) NULL,
            Aliases         NVARCHAR(500) NULL,
            DataSourceId    INT NULL,
            IsActive        BIT NOT NULL DEFAULT 1,
            CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
        );
    END");
    }

    /// <summary>
    /// Golden Memory: Onaylanmış AI girdi/çıktı kalıplarını saklayan AiPatterns tablosunu oluşturur.
    /// </summary>
    private static async Task PrepareAiPatternsAsync(SqlConnection conn)
    {
        await conn.ExecuteAsync(@"
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AiPatterns')
    BEGIN
        CREATE TABLE AiPatterns (
            Id              INT IDENTITY(1,1) PRIMARY KEY,
            SkillId         NVARCHAR(100) NOT NULL,
            PatternType     NVARCHAR(50) NOT NULL,
            InputPattern    NVARCHAR(500) NOT NULL,
            ApprovedOutput  NVARCHAR(MAX) NOT NULL,
            UsageCount      INT NOT NULL DEFAULT 0,
            LastUsedAt      DATETIME2 NULL,
            ApprovedBy      INT NULL,
            CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
        );
        CREATE INDEX IX_AiPatterns_SkillId ON AiPatterns (SkillId);
    END");
    }
}
