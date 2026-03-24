using Dapper;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Data.SqlClient;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

/// <summary>
/// Sistemdeki dosya eklerini (Attachments), yükleme ve indirme işlemlerini yöneten servis sınıfıdır.
/// </summary>
public class AttachmentService(IConfiguration config, AuditService auditService, IWebHostEnvironment env) 
    : BaseService(config, auditService)
{
    private readonly string _uploadPath = Path.Combine(env.WebRootPath, "uploads");

    public async Task<ServiceResult<FileAttachment?>> GetAttachmentByIdAsync(int id)
    {
        return await ExecuteServiceAsync<FileAttachment?>(async conn =>
        {
            var sql = "SELECT * FROM FileAttachments WHERE Id = @Id";
            return await conn.QueryFirstOrDefaultAsync<FileAttachment>(sql, new { Id = id });
        });
    }

    /// <summary>
    /// Belirli bir varlığa (Görev, Toplantı vb.) ait dosya eklerini listeler.
    /// </summary>
    public async Task<ServiceResult<List<FileAttachment>>> GetAttachmentsAsync(string entityType, int entityId)
    {
        return await ExecuteServiceAsync<List<FileAttachment>>(async conn =>
        {
            try
            {
                return (await conn.QueryAsync<FileAttachment>(
                    "sp_Attachment_List",
                    new { EntityType = entityType, EntityId = entityId },
                    commandType: System.Data.CommandType.StoredProcedure)).ToList();
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                var sql = "SELECT * FROM FileAttachments WHERE RelatedEntityType = @EntityType AND RelatedEntityId = @EntityId ORDER BY CreatedAt DESC";
                return (await conn.QueryAsync<FileAttachment>(sql, new { EntityType = entityType, EntityId = entityId })).ToList();
            }
        });
    }

    /// <summary>
    /// Fiziksel bir dosyayı sunucuya kaydeder ve veritabanı eşleşmesini oluşturur.
    /// </summary>
    public async Task<ServiceResult<int>> UploadAttachmentAsync(IBrowserFile file, string entityType, int entityId, int? userId)
    {
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }

        // Benzersiz bir dosya adı oluştur (Çakışmaları önlemek için)
        var trustedFileName = Path.GetRandomFileName() + Path.GetExtension(file.Name);
        var fullPath = Path.Combine(_uploadPath, trustedFileName);

        try
        {
            await using FileStream fs = new(fullPath, FileMode.Create);
            await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(fs); // Max 10MB
        }
        catch (Exception ex)
        {
            return ServiceResult<int>.Failure($"Dosya fiziksel olarak kaydedilemedi: {ex.Message}");
        }

        return await ExecuteServiceAsync<int>(async conn =>
        {
            var attachment = new
            {
                FileName = file.Name,
                StoragePath = "/uploads/" + trustedFileName,
                file.ContentType,
                FileSize = file.Size,
                RelatedEntityType = entityType,
                RelatedEntityId = entityId,
                CreatedByUserId = userId
            };

            try
            {
                return await conn.ExecuteScalarAsync<int>(
                    "sp_Attachment_Add",
                    attachment,
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                var sql = @"
                    INSERT INTO FileAttachments (FileName, StoragePath, ContentType, FileSize, RelatedEntityType, RelatedEntityId, CreatedByUserId)
                    OUTPUT INSERTED.Id
                    VALUES (@FileName, @StoragePath, @ContentType, @FileSize, @RelatedEntityType, @RelatedEntityId, @CreatedByUserId)";

                return await conn.ExecuteScalarAsync<int>(sql, attachment);
            }
        });
    }

    /// <summary>
    /// Bir dosya ekini sistemden (hem DB hem disk) tamamen siler.
    /// </summary>
    public async Task<ServiceResult> DeleteAttachmentAsync(int id)
    {
        var result = await ExecuteServiceAsync<FileAttachment?>(async conn => 
            await conn.QueryFirstOrDefaultAsync<FileAttachment>("SELECT * FROM FileAttachments WHERE Id = @Id", new { Id = id }));

        if (!result.IsSuccess || result.Data == null)
            return ServiceResult.Failure("Dosya kaydı bulunamadı.");

        var attachment = result.Data;

        // DB'den sil
        var dbResult = await ExecuteServiceAsync(async conn =>
        {
            try
            {
                await conn.ExecuteAsync("sp_Attachment_Delete", new { Id = id }, commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                await conn.ExecuteAsync("DELETE FROM FileAttachments WHERE Id = @Id", new { Id = id });
            }
        });

        if (dbResult.IsSuccess)
        {
            // Diskten sil
            var fullPath = Path.Combine(env.WebRootPath, attachment.StoragePath.TrimStart('/'));
            if (File.Exists(fullPath))
            {
                try { File.Delete(fullPath); } catch { /* Log but ignore */ }
            }
        }

        return dbResult;
    }
}
