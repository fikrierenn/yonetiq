using Microsoft.EntityFrameworkCore;
using YonetIQ.Data.Models;

namespace YonetIQ.Data;

/// <summary>
/// Entity Framework Core Veritabanı Bağlamı (DbContext).
/// Bu sınıf, veritabanı tabloları ile C# modelleri arasındaki eşleşmeyi sağlar.
/// Genellikle şema yönetimi (Migrations) ve temel CRUD işlemleri için kullanılır.
/// Performans gerektiren raporlama işlerinde Dapper tercih edilmektedir.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>Toplantı kayıtlarını temsil eden tablo.</summary>
    public DbSet<Meeting> Meetings => Set<Meeting>();

    /// <summary>Toplantılarda alınan kararları temsil eden tablo.</summary>
    public DbSet<Decision> Decisions => Set<Decision>();

    /// <summary>Görev (iş) kayıtlarını temsil eden tablo.</summary>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Reflection", "IL2026", Justification = "Dapper usage")]
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    /// <summary>Sistem kullanıcılarını temsil eden tablo.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Şirket içi yazışma ve bildirimleri temsil eden tablo.</summary>
    public DbSet<CommunicationMessage> CommunicationMessages => Set<CommunicationMessage>();

    /// <summary>Dosya eklerini (Attachments) temsil eden tablo.</summary>
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();

    /// <summary>Rapor/Sorgu tanımlarını temsil eden tablo.</summary>
    public DbSet<QueryRecord> QueryRecords => Set<QueryRecord>();

    /// <summary>Dashboard/Sorgu Seti tanımlarını temsil eden tablo.</summary>
    public DbSet<QuerySet> QuerySets => Set<QuerySet>();

    /// <summary>Setler ile Sorgular arasındaki ilişki tablosu.</summary>
    public DbSet<SetQuery> SetQueries => Set<SetQuery>();

    /// <summary>Kullanıcıların favori raporlarını tutan tablo.</summary>
    public DbSet<ReportFavorite> ReportFavorites => Set<ReportFavorite>();

    /// <summary>Dinamik tanımları (Lookup/Sözlük) tutan tablo.</summary>
    public DbSet<Lookup> Lookups => Set<Lookup>();

    /// <summary>Rapor paylaşımlarını / yetkilendirmelerini tutan tablo.</summary>
    public DbSet<ReportShare> ReportShares => Set<ReportShare>();

    /// <summary>Sistem denetim izlerini (Log) tutan tablo.</summary>
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    /// <summary>Takvim özel günlerini tutan tablo.</summary>
    public DbSet<CalendarSpecialDay> CalendarSpecialDays => Set<CalendarSpecialDay>();

    /// <summary>Kullanıcıların kişisel notlarını tutan tablo.</summary>
    public DbSet<PersonalNote> PersonalNotes => Set<PersonalNote>();

    /// <summary>Çoklu sunucu bağlantı tanımlarını tutan tablo.</summary>
    public DbSet<DataSource> DataSources => Set<DataSource>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // QueryRecord.ResultType ve QuerySet.Type artık doğrudan string — enum dönüşümü gerekmez
        mb.Entity<QueryRecord>().Property(s => s.ResultType).HasMaxLength(50);
        mb.Entity<QuerySet>().Property(s => s.Type).HasMaxLength(50);

        // DataSource: EncryptedConnectionString büyük metin olabilir
        mb.Entity<DataSource>().Property(d => d.EncryptedConnectionString).HasColumnType("nvarchar(MAX)");
        // QueryRecord → DataSource FK (silinen kaynak → null bırak)
        mb.Entity<QueryRecord>()
            .HasOne<DataSource>()
            .WithMany()
            .HasForeignKey(q => q.DataSourceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Büyük metin alanları için nvarchar(MAX) tanımlamaları
        mb.Entity<Meeting>().Property(t => t.Agenda).HasColumnType("nvarchar(MAX)");
        mb.Entity<Meeting>().Property(t => t.Notes).HasColumnType("nvarchar(MAX)");
        mb.Entity<Decision>().Property(k => k.Content).HasColumnType("nvarchar(MAX)");
        mb.Entity<TaskItem>().Property(g => g.Description).HasColumnType("nvarchar(MAX)");
        mb.Entity<QueryRecord>().Property(s => s.SqlContent).HasColumnType("nvarchar(MAX)");
        mb.Entity<PersonalNote>().Property(n => n.Content).HasColumnType("nvarchar(MAX)");

        // Toplantı - Karar İlişkisi
        mb.Entity<Decision>()
            .HasOne<Meeting>()
            .WithMany(t => t.Decisions)
            .HasForeignKey(k => k.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Karar - Sorumlu Kullanıcı İlişkisi
        mb.Entity<Decision>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(k => k.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Karar - Bağımlı Görev İlişkisi
        mb.Entity<Decision>()
            .HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(k => k.LinkedTaskId)
            .OnDelete(DeleteBehavior.SetNull);

        // Görev - Atanan Kullanıcı İlişkisi
        mb.Entity<TaskItem>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(g => g.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Set - Sorgu İlişkisi (Mapping)
        mb.Entity<SetQuery>()
            .HasOne<QuerySet>()
            .WithMany()
            .HasForeignKey(ss => ss.SetId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<SetQuery>()
            .HasOne<QueryRecord>()
            .WithMany()
            .HasForeignKey(ss => ss.QueryId)
            .OnDelete(DeleteBehavior.Cascade);

        // ReportFavorite Composite PK ve İlişkiler
        mb.Entity<ReportFavorite>()
            .HasKey(rf => new { rf.UserId, rf.QueryId });

        mb.Entity<ReportFavorite>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(rf => rf.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<ReportFavorite>()
            .HasOne<QueryRecord>()
            .WithMany()
            .HasForeignKey(rf => rf.QueryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
