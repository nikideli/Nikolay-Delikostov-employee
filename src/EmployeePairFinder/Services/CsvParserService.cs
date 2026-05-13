using System.IO;
using EmployeePairFinder.Enums;
using EmployeePairFinder.Models;

namespace EmployeePairFinder.Services;

// Reads the CSV file, skips the header if detected, and converts each valid row
// into an EmployeeRecord. Invalid rows are skipped and recorded as warnings.
public sealed class CsvParserService : ICsvParserService
{
    private readonly IFileValidatorService _validator;
    private readonly IDateParserService    _dateParser;

    public CsvParserService(
        IFileValidatorService validator,
        IDateParserService    dateParser)
    {
        _validator  = validator;
        _dateParser = dateParser;
    }

    /// <inheritdoc/>
    public CsvParseResult Parse(string filePath, DateFormatPreference preference)
    {
        // ── File-level validation ─────────────────────────────────────────────
        // Throws nothing here; any blocking error is returned via the result model
        // so the ViewModel can display it in the error panel.
        var validation = _validator.Validate(filePath);
        if (!validation.IsValid)
        {
            // Return an empty result; the caller must check Warnings / ErrorMessage.
            // We embed the error as the first (and only) entry so callers using a
            // single Warnings list still see it. Fatal errors are handled at a higher
            // level, but we still surface them here for completeness.
            return new CsvParseResult
            {
                Records  = [],
                Warnings = [$"[FILE ERROR] {validation.ErrorMessage}"]
            };
        }

        // ── Read all lines ────────────────────────────────────────────────────
        var allLines = File.ReadAllLines(filePath)
                           .Where(l => !string.IsNullOrWhiteSpace(l))
                           .ToArray();

        // Skip the header row if the validator detected one
        var dataLines = validation.HasHeader ? allLines.Skip(1) : allLines;

        var records  = new List<EmployeeRecord>();
        var warnings = new List<string>();
        int lineNumber = validation.HasHeader ? 2 : 1; // 1-based for user messages

        foreach (var line in dataLines)
        {
            ParseLine(line, lineNumber, preference, records, warnings);
            lineNumber++;
        }

        return new CsvParseResult
        {
            Records  = records.AsReadOnly(),
            Warnings = warnings.AsReadOnly()
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    // Validates and parses a single row. Adds to records on success, warnings on failure.
    private void ParseLine(
        string line,
        int lineNumber,
        DateFormatPreference preference,
        List<EmployeeRecord> records,
        List<string> warnings)
    {
        var fields = line.Split(',').Select(f => f.Trim()).ToArray();

        // ── Column count guard ────────────────────────────────────────────────
        if (fields.Length < 4)
        {
            var msg = $"Row {lineNumber}: Expected 4 columns, found {fields.Length}. Row skipped. Content: \"{line}\"";
            warnings.Add(msg);
            Console.WriteLine($"[WARN] {msg}");
            return;
        }

        // ── EmpID ─────────────────────────────────────────────────────────────
        if (!int.TryParse(fields[0], out int empId))
        {
            var msg = $"Row {lineNumber}: Invalid Employee ID \"{fields[0]}\" — must be an integer. Row skipped.";
            warnings.Add(msg);
            Console.WriteLine($"[WARN] {msg}");
            return;
        }

        // ── ProjectID ─────────────────────────────────────────────────────────
        if (!int.TryParse(fields[1], out int projectId))
        {
            var msg = $"Row {lineNumber}: Invalid Project ID \"{fields[1]}\" — must be an integer. Row skipped.";
            warnings.Add(msg);
            Console.WriteLine($"[WARN] {msg}");
            return;
        }

        // ── DateFrom ──────────────────────────────────────────────────────────
        DateTime dateFrom;
        try
        {
            var parsed = _dateParser.Parse(fields[2], preference, out string? ambigFrom);
            if (parsed is null)
            {
                // DateFrom cannot be null/empty — it is the assignment start date
                var msg = $"Row {lineNumber}: DateFrom is empty or NULL, which is not allowed. Row skipped.";
                warnings.Add(msg);
                Console.WriteLine($"[WARN] {msg}");
                return;
            }
            dateFrom = parsed.Value;

            if (ambigFrom is not null)
            {
                var warnMsg = $"Row {lineNumber} DateFrom: {ambigFrom}";
                warnings.Add(warnMsg);
                Console.WriteLine($"[WARN] {warnMsg}");
            }
        }
        catch (FormatException ex)
        {
            var msg = $"Row {lineNumber}: Cannot parse DateFrom \"{fields[2]}\" — {ex.Message}. Row skipped.";
            warnings.Add(msg);
            Console.WriteLine($"[WARN] {msg}");
            return;
        }

        // ── DateTo (nullable) ─────────────────────────────────────────────────
        DateTime? dateTo;
        try
        {
            dateTo = _dateParser.Parse(fields[3], preference, out string? ambigTo);

            if (ambigTo is not null)
            {
                var warnMsg = $"Row {lineNumber} DateTo: {ambigTo}";
                warnings.Add(warnMsg);
                Console.WriteLine($"[WARN] {warnMsg}");
            }
        }
        catch (FormatException ex)
        {
            var msg = $"Row {lineNumber}: Cannot parse DateTo \"{fields[3]}\" — {ex.Message}. Row skipped.";
            warnings.Add(msg);
            Console.WriteLine($"[WARN] {msg}");
            return;
        }

        // ── Date range sanity ─────────────────────────────────────────────────
        // Effective end date resolves NULL to today before comparison
        var effectiveTo = dateTo ?? DateTime.Today;
        if (dateFrom > effectiveTo)
        {
            var msg = $"Row {lineNumber}: DateFrom ({dateFrom:yyyy-MM-dd}) is after DateTo " +
                      $"({effectiveTo:yyyy-MM-dd}). Row skipped.";
            warnings.Add(msg);
            Console.WriteLine($"[WARN] {msg}");
            return;
        }

        records.Add(new EmployeeRecord
        {
            EmpId     = empId,
            ProjectId = projectId,
            DateFrom  = dateFrom,
            DateTo    = dateTo
        });
    }
}
