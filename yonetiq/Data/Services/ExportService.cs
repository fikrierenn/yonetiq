using System.Text;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

/// <summary>
/// Rapor sonuçlarını Excel (XLSX) formatına dönüştürür.
/// </summary>
public class ExportService
{
    static ExportService()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// QueryResult verisini CSV (semicolon-separated, UTF-8 BOM) byte dizisine dönüştürür.
    /// Excel'de Türkçe karakterlerin doğru görünmesi için BOM eklenir.
    /// </summary>
    public byte[] ExportToCsv(QueryResult data)
    {
        var sb = new StringBuilder();

        // Başlık satırı
        sb.AppendLine(string.Join(";", data.Columns.Select(c => EscapeCsv(c))));

        // Veri satırları
        foreach (var row in data.Rows)
        {
            var values = data.Columns.Select(col =>
            {
                var val = row.TryGetValue(col, out var v) ? v : null;
                var text = val switch
                {
                    DateTime dt => dt.ToString("dd.MM.yyyy HH:mm"),
                    DateTimeOffset dto => dto.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    _ => val?.ToString() ?? ""
                };
                return EscapeCsv(text);
            });
            sb.AppendLine(string.Join(";", values));
        }

        // BOM + UTF-8 — Excel Türkçe karakterleri doğru açar
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        return bom.Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(';') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    /// <summary>
    /// QueryResult verisini XLSX byte dizisine dönüştürür.
    /// </summary>
    public byte[] BuildExcel(QueryResult data, string reportName)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Rapor");

        // ── Başlık satırı ──────────────────────────────────────
        for (int c = 0; c < data.Columns.Count; c++)
        {
            var cell = ws.Cells[1, c + 1];
            cell.Value = data.Columns[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0xE3, 0x1E, 0x24));
            cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        // ── Veri satırları ─────────────────────────────────────
        for (int r = 0; r < data.Rows.Count; r++)
        {
            var row = data.Rows[r];
            for (int c = 0; c < data.Columns.Count; c++)
            {
                var colName = data.Columns[c];
                var value = row.TryGetValue(colName, out var v) ? v : null;
                ws.Cells[r + 2, c + 1].Value = value switch
                {
                    DateTime dt => dt.ToString("dd.MM.yyyy HH:mm"),
                    DateTimeOffset dto => dto.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    _ => value?.ToString()
                };
            }

            // Çift satır arka planı
            if (r % 2 == 1)
            {
                var rowRange = ws.Cells[r + 2, 1, r + 2, data.Columns.Count];
                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0xFF, 0xF5, 0xF5));
            }
        }

        // ── Sütun genişliği otomatik ───────────────────────────
        ws.Cells[ws.Dimension?.Address ?? "A1"].AutoFitColumns(8, 50);

        // ── Sayfanın üstüne rapor adı ──────────────────────────
        ws.HeaderFooter.OddHeader.CenteredText = reportName;
        ws.HeaderFooter.OddFooter.RightAlignedText = $"YonetIQ — {DateTime.Now:dd.MM.yyyy}";

        return package.GetAsByteArray();
    }

    /// <summary>
    /// QueryResult verisini yazdırılabilir HTML tablosuna dönüştürür.
    /// </summary>
    public string BuildHtmlReport(QueryResult data, string reportName)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"<h1>{System.Net.WebUtility.HtmlEncode(reportName)}</h1>");
        sb.Append($"<p style='color:#666;font-size:11px'>Oluşturuldu: {DateTime.Now:dd.MM.yyyy HH:mm} | Satır sayısı: {data.Rows.Count}</p>");
        sb.Append("<table><thead><tr>");
        foreach (var col in data.Columns)
            sb.Append($"<th>{System.Net.WebUtility.HtmlEncode(col)}</th>");
        sb.Append("</tr></thead><tbody>");
        foreach (var row in data.Rows)
        {
            sb.Append("<tr>");
            foreach (var col in data.Columns)
            {
                var val = row.TryGetValue(col, out var v) ? v : null;
                var text = val switch {
                    DateTime dt => dt.ToString("dd.MM.yyyy HH:mm"),
                    DateTimeOffset dto => dto.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    _ => val?.ToString() ?? ""
                };
                sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(text)}</td>");
            }
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");
        sb.Append("<div class='footer'>YonetIQ — Kurumsal Yönetim Portalı</div>");
        return sb.ToString();
    }
}
