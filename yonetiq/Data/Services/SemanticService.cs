using Dapper;
using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

public class SemanticService(IConfiguration config, AuditService? auditService) : BaseService(config, auditService)
{
    public async Task<ServiceResult<List<SemanticDefinition>>> ListAsync()
    {
        return await ExecuteServiceAsync<List<SemanticDefinition>>(async conn =>
        {
            var result = await conn.QueryAsync<SemanticDefinition>(@"
                SELECT Id, TermType, TableName, ColumnName, BusinessName, Description,
                       SqlExpression, Aliases, DataSourceId, IsActive, CreatedAt
                FROM SemanticDefinitions WHERE IsActive = 1
                ORDER BY TermType, BusinessName");
            return result.ToList();
        });
    }

    public async Task<ServiceResult<List<SemanticDefinition>>> SearchAsync(string term)
    {
        return await ExecuteServiceAsync<List<SemanticDefinition>>(async conn =>
        {
            var result = await conn.QueryAsync<SemanticDefinition>(@"
                SELECT Id, TermType, TableName, ColumnName, BusinessName, Description,
                       SqlExpression, Aliases, DataSourceId, IsActive, CreatedAt
                FROM SemanticDefinitions
                WHERE IsActive = 1
                AND (BusinessName LIKE @term OR Aliases LIKE @term OR Description LIKE @term)
                ORDER BY TermType, BusinessName",
                new { term = $"%{term}%" });
            return result.ToList();
        });
    }

    public async Task<ServiceResult<int>> SaveAsync(SemanticDefinition def)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            if (def.Id == 0)
            {
                return await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO SemanticDefinitions (TermType, TableName, ColumnName, BusinessName, Description, SqlExpression, Aliases, DataSourceId, IsActive)
                    VALUES (@TermType, @TableName, @ColumnName, @BusinessName, @Description, @SqlExpression, @Aliases, @DataSourceId, @IsActive);
                    SELECT CAST(SCOPE_IDENTITY() AS INT)", def);
            }
            else
            {
                await conn.ExecuteAsync(@"
                    UPDATE SemanticDefinitions
                    SET TermType=@TermType, TableName=@TableName, ColumnName=@ColumnName, BusinessName=@BusinessName,
                        Description=@Description, SqlExpression=@SqlExpression, Aliases=@Aliases,
                        DataSourceId=@DataSourceId, IsActive=@IsActive
                    WHERE Id=@Id", def);
                return def.Id;
            }
        });
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync("DELETE FROM SemanticDefinitions WHERE Id=@id", new { id });
        });
    }
}
