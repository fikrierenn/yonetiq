namespace YonetIQ.Data.Models;

/// <summary>Schema keşif sonucu — tablolar, kolonlar, aday tespitler.</summary>
public class SchemaDiscoveryResult
{
    public int DataSourceId { get; set; }
    public DateTime DiscoveredAt { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public List<TableInfo> Tables { get; set; } = [];
    public List<string> SalesCandidates { get; set; } = [];
    public List<string> ProductCandidates { get; set; } = [];
    public List<string> StoreCandidates { get; set; } = [];
}

public class TableInfo
{
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public List<ColumnInfo> Columns { get; set; } = [];
    public List<Dictionary<string, string>> SampleValues { get; set; } = [];
}

public class ColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string IsNullable { get; set; } = "YES";
    public int? MaxLength { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public string? ReferencedTable { get; set; }
}

public class SchemaQuestion
{
    public string Id { get; set; } = string.Empty;
    public QuestionPriority Priority { get; set; }
    public string Question { get; set; } = string.Empty;
    public string? Context { get; set; }
    public List<string>? Options { get; set; }
    public bool Answered { get; set; }
}

public class SchemaAnswer
{
    public string QuestionId { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class SchemaMapping
{
    public int DataSourceId { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public DateTime MappedAt { get; set; }
    public List<SchemaAnswer> Answers { get; set; } = [];
    public List<TableSummary> TableSummary { get; set; } = [];
}

public class TableSummary
{
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public int ColumnCount { get; set; }
    public bool IsSalesTable { get; set; }
    public bool IsProductTable { get; set; }
    public bool IsStoreTable { get; set; }
}

public enum QuestionPriority { Low = 0, Medium = 1, High = 2, Critical = 3 }
