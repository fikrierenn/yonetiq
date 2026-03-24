using YonetIQ.Data.Models;

namespace YonetIQ.Data.Infrastructure;

/// <summary>
/// Sorgu sonuçlarında deterministik istatistiksel analiz yapar.
/// AI çağrısı yapmaz — pure math (z-score, IQR, delta hesaplama).
/// </summary>
public static class StatisticalAnalyzer
{
    /// <summary>
    /// Sayısal kolonlardaki outlier'ları IQR (Interquartile Range) yöntemiyle tespit eder.
    /// IQR: Q3-Q1 aralığının 1.5 katı dışındaki değerler outlier kabul edilir.
    /// </summary>
    public static List<AnomalyResult> DetectOutliers(QueryResult result, double threshold = 1.5)
    {
        var anomalies = new List<AnomalyResult>();
        if (result.Rows.Count < 4) return anomalies; // IQR için en az 4 satır gerek

        foreach (var column in result.Columns)
        {
            var numericValues = ExtractNumericValues(result, column);
            if (numericValues.Count < 4) continue;

            var sorted = numericValues.OrderBy(v => v.Value).ToList();
            var q1 = Percentile(sorted.Select(v => v.Value).ToList(), 25);
            var q3 = Percentile(sorted.Select(v => v.Value).ToList(), 75);
            var iqr = q3 - q1;

            if (iqr == 0) continue; // Tüm değerler aynı

            var lowerBound = q1 - (threshold * iqr);
            var upperBound = q3 + (threshold * iqr);

            foreach (var (rowIndex, value) in numericValues)
            {
                if (value < lowerBound || value > upperBound)
                {
                    anomalies.Add(new AnomalyResult
                    {
                        Column = column,
                        RowIndex = rowIndex,
                        Value = value,
                        LowerBound = lowerBound,
                        UpperBound = upperBound,
                        Severity = Math.Abs(value - (value < lowerBound ? lowerBound : upperBound)) / iqr > 3
                            ? "Kritik" : "Dikkat",
                        Description = value < lowerBound
                            ? $"{column} kolonu satır {rowIndex + 1}: {value:N2} beklenenden düşük (alt sınır: {lowerBound:N2})"
                            : $"{column} kolonu satır {rowIndex + 1}: {value:N2} beklenenden yüksek (üst sınır: {upperBound:N2})"
                    });
                }
            }
        }

        return anomalies.OrderByDescending(a => a.Severity == "Kritik").ToList();
    }

    /// <summary>
    /// İki QueryResult arasındaki delta'yı hesaplar (aynı sorgunun farklı çalıştırmaları).
    /// Aynı kolon yapısına sahip olmalıdır.
    /// </summary>
    public static DeltaReport ComparePrevious(QueryResult current, QueryResult previous)
    {
        var report = new DeltaReport
        {
            CurrentRowCount = current.RowCount,
            PreviousRowCount = previous.RowCount,
            RowCountDelta = current.RowCount - previous.RowCount
        };

        // Ortak sayısal kolonları karşılaştır
        var commonColumns = current.Columns.Intersect(previous.Columns).ToList();

        foreach (var column in commonColumns)
        {
            var currentValues = ExtractNumericValues(current, column);
            var previousValues = ExtractNumericValues(previous, column);

            if (currentValues.Count == 0 || previousValues.Count == 0) continue;

            var currentSum = currentValues.Sum(v => v.Value);
            var previousSum = previousValues.Sum(v => v.Value);
            var delta = currentSum - previousSum;
            var percentChange = previousSum != 0 ? (delta / previousSum) * 100 : 0;

            report.ColumnDeltas.Add(new ColumnDelta
            {
                Column = column,
                CurrentTotal = currentSum,
                PreviousTotal = previousSum,
                Delta = delta,
                PercentChange = percentChange,
                Trend = delta > 0 ? "Yükseliş" : delta < 0 ? "Düşüş" : "Stabil"
            });
        }

        return report;
    }

    private static List<(int RowIndex, double Value)> ExtractNumericValues(QueryResult result, string column)
    {
        var values = new List<(int, double)>();
        for (var i = 0; i < result.Rows.Count; i++)
        {
            if (result.Rows[i].TryGetValue(column, out var val) && val is not null)
            {
                if (double.TryParse(val.ToString(), out var num) && num != 0)
                {
                    values.Add((i, num));
                }
            }
        }
        return values;
    }

    private static double Percentile(List<double> sorted, int percentile)
    {
        var index = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper) return sorted[lower];
        return sorted[lower] + (index - lower) * (sorted[upper] - sorted[lower]);
    }
}

public class AnomalyResult
{
    public string Column { get; set; } = string.Empty;
    public int RowIndex { get; set; }
    public double Value { get; set; }
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public string Severity { get; set; } = "Dikkat";
    public string Description { get; set; } = string.Empty;
}

public class DeltaReport
{
    public int CurrentRowCount { get; set; }
    public int PreviousRowCount { get; set; }
    public int RowCountDelta { get; set; }
    public List<ColumnDelta> ColumnDeltas { get; set; } = [];

    public string ToSummary()
    {
        var lines = new List<string>
        {
            $"📊 Satır sayısı: {PreviousRowCount} → {CurrentRowCount} ({(RowCountDelta >= 0 ? "+" : "")}{RowCountDelta})"
        };

        foreach (var d in ColumnDeltas.Where(d => d.Delta != 0))
        {
            var icon = d.Delta > 0 ? "📈" : "📉";
            lines.Add($"{icon} {d.Column}: {d.PreviousTotal:N2} → {d.CurrentTotal:N2} ({d.Trend}, {d.PercentChange:+0.0;-0.0}%)");
        }

        return lines.Count > 1 ? string.Join("\n", lines) : "Anlamlı değişiklik tespit edilmedi.";
    }
}

public class ColumnDelta
{
    public string Column { get; set; } = string.Empty;
    public double CurrentTotal { get; set; }
    public double PreviousTotal { get; set; }
    public double Delta { get; set; }
    public double PercentChange { get; set; }
    public string Trend { get; set; } = "Stabil";
}
