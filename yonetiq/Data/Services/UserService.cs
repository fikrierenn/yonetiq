using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Kullanıcı yönetimi işlemlerini yürüten servis sınıfıdır.
/// </summary>
public class UserService(IConfiguration config, AuditService auditService) : BaseService(config, auditService)
{
    /// <summary>
    /// Sistemde kayıtlı kullanıcıları listeler.
    /// </summary>
    /// <param name="onlyActive">Sadece aktif kullanıcıları getirmek için true set edilmelidir.</param>
    public async Task<ServiceResult<List<User>>> GetAllAsync(bool onlyActive = true)
    {
        return await ExecuteServiceAsync<List<User>>(async conn =>
        {
            try
            {
                return (await conn.QueryAsync<User>(
                    "sp_User_List",
                    new { OnlyActive = onlyActive },
                    commandType: System.Data.CommandType.StoredProcedure)).ToList();
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                var sql = "SELECT * FROM Users " + (onlyActive ? "WHERE IsActive = 1" : "") + " ORDER BY FullName";
                return (await conn.QueryAsync<User>(sql)).ToList();
            }
        });
    }

    /// <summary>
    /// Belirtilen ID'ye sahip kullanıcıyı getirir.
    /// </summary>
    public async Task<ServiceResult<User?>> GetByIdAsync(int id)
    {
        return await ExecuteServiceAsync<User?>(async conn =>
        {
            try
            {
                return await conn.QueryFirstOrDefaultAsync<User>(
                    "sp_User_Get",
                    new { Id = id },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                return await conn.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE Id = @Id", new { Id = id });
            }
        });
    }

    /// <summary>
    /// Yeni bir kullanıcı kaydeder veya mevcut kullanıcıyı günceller.
    /// </summary>
    /// <param name="user">Kaydedilecek kullanıcı nesnesi.</param>
    public async Task<ServiceResult<int>> SaveAsync(User user)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            int resultId;
            try
            {
                resultId = await conn.ExecuteScalarAsync<int>(
                    "sp_User_Save",
                    new
                    {
                        user.Id,
                        user.FullName,
                        user.Email,
                        RoleLookupId = user.RoleLookupId,
                        user.IsActive,
                        user.CreatedAt,
                        user.DepartmentLookupId
                    },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                if (user.Id == 0)
                {
                    var sql = @"
                        INSERT INTO Users (FullName, Email, RoleLookupId, IsActive, CreatedAt, DepartmentLookupId)
                        OUTPUT INSERTED.Id
                        VALUES (@FullName, @Email, @RoleLookupId, @IsActive, @CreatedAt, @DepartmentLookupId)";
                    resultId = await conn.ExecuteScalarAsync<int>(sql, user);
                }
                else
                {
                    var sql = @"
                        UPDATE Users
                        SET FullName = @FullName, 
                            Email = @Email, 
                            RoleLookupId = @RoleLookupId, 
                            IsActive = @IsActive, 
                            DepartmentLookupId = @DepartmentLookupId
                        WHERE Id = @Id";
                    await conn.ExecuteAsync(sql, user);
                    resultId = user.Id;
                }
            }

            LogAction("User", user.Id > 0 ? "Update" : "Add", new { Id = resultId, FullName = user.FullName });
            return resultId;
        });
    }

    /// <summary>
    /// Kullanıcının Telegram Chat ID'sini günceller veya temizler.
    /// </summary>
    public async Task<ServiceResult> UpdateTelegramChatIdAsync(int userId, string? chatId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "UPDATE Users SET TelegramChatId = @ChatId WHERE Id = @UserId",
                new { ChatId = chatId, UserId = userId });
            LogAction("User", "UpdateTelegram", new { UserId = userId, Linked = chatId is not null });
        });
    }

    /// <summary>
    /// Belirtilen kullanıcıyı sistemden siler.
    /// </summary>
    public async Task<ServiceResult> DeleteAsync(int id)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            try
            {
                await conn.ExecuteAsync(
                    "sp_User_Delete",
                    new { Id = id },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                await conn.ExecuteAsync("DELETE FROM Users WHERE Id = @Id", new { Id = id });
            }
            LogAction("User", "Delete", new { Id = id });
        });
    }

    /// <summary>
    /// Belirtilen departmana ait aktif kullanıcıların (Id, FullName) listesini döner.
    /// Departman bazlı erişim kontrolünde kullanılır.
    /// </summary>
    public async Task<List<(int Id, string FullName)>> GetUsersByDepartmentAsync(int departmentLookupId)
    {
        var result = await ExecuteServiceAsync<List<(int, string)>>(async conn =>
        {
            var rows = await conn.QueryAsync<(int Id, string FullName)>(
                @"SELECT Id, FullName FROM Users
                  WHERE DepartmentLookupId = @DeptId AND IsActive = 1",
                new { DeptId = departmentLookupId });
            return rows.ToList();
        });
        return result.Data ?? [];
    }
}

