using EmployeesApi.Models;
using System.Globalization;

namespace EmployeesApi.Services;

public class CsvParserService : ICsvParserService
{
    // Ordered from most specific to most general to avoid ambiguous parses
    private static readonly string[] DateFormats =
    [
        // ISO variants
        "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd", "yyyyMMdd", "yyyy-M-d", "yyyy/M/d",
        // European dd/mm/yyyy
        "dd/MM/yyyy", "dd-MM-yyyy", "dd.MM.yyyy", "d/M/yyyy", "d-M-yyyy", "d.M.yyyy",
        "dd/MM/yy",   "dd-MM-yy",
        // US mm/dd/yyyy
        "MM/dd/yyyy", "MM-dd-yyyy", "MM.dd.yyyy", "M/d/yyyy",  "M-d-yyyy",
        "MM/dd/yy",   "M/d/yy",
        // Long month name
        "dd MMM yyyy", "dd MMMM yyyy", "MMM dd yyyy", "MMMM dd yyyy",
        "MMM dd, yyyy", "MMMM dd, yyyy",
        // With time (ignored)
        "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss",
    ];

    public List<EmployeeRecord> Parse(Stream csvStream)
    {
        var records = new List<EmployeeRecord>();
        using var reader = new StreamReader(csvStream, detectEncodingFromByteOrderMarks: true);

        bool isFirstLine = true;
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // Skip a header row (first line whose first token is not a number)
            if (isFirstLine)
            {
                isFirstLine = false;
                if (!char.IsDigit(line[0])) continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 4) continue;

            if (!int.TryParse(parts[0].Trim(), out int empId)) continue;
            if (!int.TryParse(parts[1].Trim(), out int projectId)) continue;
            if (!TryParseDate(parts[2].Trim(), out DateTime dateFrom)) continue;

            string dateToStr = parts[3].Trim();
            DateTime dateTo;
            if (string.IsNullOrEmpty(dateToStr) ||
                dateToStr.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                dateTo = DateTime.Today;
            }
            else if (!TryParseDate(dateToStr, out dateTo))
            {
                continue;
            }

            records.Add(new EmployeeRecord
            {
                EmpID = empId,
                ProjectID = projectId,
                DateFrom = dateFrom,
                DateTo = dateTo
            });
        }

        return records;
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        // Try explicit formats first for deterministic parsing
        if (DateTime.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, 
            DateTimeStyles.None, out date))
        {
            return true;
        }

        // Fall back to .NET's built-in parser (handles RFC, culture-specific, etc.)
        return DateTime.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces, out date);
    }
}
