using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Tests.Unit.Infrastructure;

public class StatisticalAnalyzerTests
{
    private static QueryResult CreateResult(string column, params double[] values)
    {
        var result = new QueryResult { Columns = [column] };
        foreach (var v in values)
            result.Rows.Add(new Dictionary<string, object?> { [column] = v });
        return result;
    }

    [Fact]
    public void DetectOutliers_NormalData_NoAnomalies()
    {
        var result = CreateResult("Sales", 10, 12, 11, 13, 10, 12, 11, 14);
        var anomalies = StatisticalAnalyzer.DetectOutliers(result);
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectOutliers_WithOutlier_DetectsIt()
    {
        var result = CreateResult("Sales", 10, 12, 11, 13, 10, 12, 11, 100);
        var anomalies = StatisticalAnalyzer.DetectOutliers(result);
        Assert.NotEmpty(anomalies);
        Assert.Contains(anomalies, a => a.Value == 100);
    }

    [Fact]
    public void DetectOutliers_TooFewRows_ReturnsEmpty()
    {
        var result = CreateResult("Sales", 10, 20, 30);
        var anomalies = StatisticalAnalyzer.DetectOutliers(result);
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectOutliers_AllSameValues_ReturnsEmpty()
    {
        var result = CreateResult("Sales", 5, 5, 5, 5, 5);
        var anomalies = StatisticalAnalyzer.DetectOutliers(result);
        Assert.Empty(anomalies);
    }

    [Fact]
    public void ComparePrevious_DetectsDelta()
    {
        var current = CreateResult("Revenue", 100, 200, 300);
        var previous = CreateResult("Revenue", 80, 150, 250);

        var delta = StatisticalAnalyzer.ComparePrevious(current, previous);

        Assert.Equal(3, delta.CurrentRowCount);
        Assert.Equal(3, delta.PreviousRowCount);
        Assert.Single(delta.ColumnDeltas);
        Assert.Equal("Yükseliş", delta.ColumnDeltas[0].Trend);
        Assert.True(delta.ColumnDeltas[0].Delta > 0);
    }

    [Fact]
    public void ComparePrevious_SameData_StabilTrend()
    {
        var current = CreateResult("Count", 10, 20, 30);
        var previous = CreateResult("Count", 10, 20, 30);

        var delta = StatisticalAnalyzer.ComparePrevious(current, previous);
        Assert.Equal("Stabil", delta.ColumnDeltas[0].Trend);
        Assert.Equal(0, delta.ColumnDeltas[0].Delta);
    }

    [Fact]
    public void DeltaReport_ToSummary_FormatsCorrectly()
    {
        var report = new DeltaReport
        {
            CurrentRowCount = 10,
            PreviousRowCount = 8,
            RowCountDelta = 2,
            ColumnDeltas =
            [
                new ColumnDelta
                {
                    Column = "Sales",
                    CurrentTotal = 1000,
                    PreviousTotal = 800,
                    Delta = 200,
                    PercentChange = 25,
                    Trend = "Yükseliş"
                }
            ]
        };

        var summary = report.ToSummary();
        Assert.Contains("8 → 10", summary);
        Assert.Contains("Sales", summary);
        Assert.Contains("Yükseliş", summary);
    }

    [Fact]
    public void AnomalyResult_HasCorrectSeverity()
    {
        // Create data with extreme outlier
        var result = CreateResult("Value", 10, 11, 12, 10, 11, 12, 10, 500);
        var anomalies = StatisticalAnalyzer.DetectOutliers(result);
        Assert.NotEmpty(anomalies);
        Assert.Contains(anomalies, a => a.Severity == "Kritik");
    }
}
