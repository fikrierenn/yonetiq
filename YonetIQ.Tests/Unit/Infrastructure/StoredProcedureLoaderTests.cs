using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Tests.Unit.Infrastructure;

public class StoredProcedureLoaderTests
{
    [Fact]
    public void ExtractCreateBlock_ValidSql_ExtractsCorrectly()
    {
        var sql = """
            -- SP: sp_Test
            IF OBJECT_ID('sp_Test', 'P') IS NOT NULL
                DROP PROCEDURE sp_Test;
            GO

            CREATE PROCEDURE sp_Test
                @Id INT
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT Id, Name FROM TestTable WHERE Id = @Id;
            END
            GO
            """;

        // ExtractCreateBlock private, test via reflection
        var method = typeof(StoredProcedureLoader)
            .GetMethod("ExtractCreateBlock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var result = (string)method.Invoke(null, [sql])!;
        Assert.Contains("CREATE PROCEDURE sp_Test", result);
        Assert.Contains("SELECT Id, Name FROM TestTable", result);
        Assert.DoesNotContain("DROP PROCEDURE", result);
        Assert.DoesNotContain("GO", result);
    }

    [Fact]
    public void ExtractCreateBlock_EmptyInput_ReturnsEmpty()
    {
        var method = typeof(StoredProcedureLoader)
            .GetMethod("ExtractCreateBlock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string)method!.Invoke(null, [""])!;
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractCreateBlock_OnlyComments_ReturnsEmpty()
    {
        var sql = """
            -- This is a comment
            -- Another comment
            """;
        var method = typeof(StoredProcedureLoader)
            .GetMethod("ExtractCreateBlock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string)method!.Invoke(null, [sql])!;
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractCreateBlock_NoGoStatements_StillWorks()
    {
        var sql = """
            CREATE PROCEDURE sp_Simple
                @Id INT
            AS
            BEGIN
                SELECT 1;
            END
            """;
        var method = typeof(StoredProcedureLoader)
            .GetMethod("ExtractCreateBlock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string)method!.Invoke(null, [sql])!;
        Assert.Contains("CREATE PROCEDURE sp_Simple", result);
        Assert.Contains("SELECT 1;", result);
    }
}
