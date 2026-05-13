using System.IO;

namespace EmployeePairFinder.Services;

// Pre-parse validation: file existence, readability, content sanity, and header detection.
public sealed class FileValidatorService : IFileValidatorService
{
    // Column header keywords that clearly identify a header row regardless of casing.
    private static readonly HashSet<string> HeaderKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "empid", "emp_id", "employeeid", "employee_id", "employee",
        "projectid", "project_id", "project",
        "datefrom", "date_from", "dateto", "date_to"
    };

    /// <inheritdoc/>
    public FileValidationResult Validate(string filePath)
    {
        // ── 1. Path sanity ────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(filePath))
            return FileValidationResult.Fail("No file path provided.");

        if (!File.Exists(filePath))
            return FileValidationResult.Fail($"File not found: \"{filePath}\".");

        // ── 2. Readable ───────────────────────────────────────────────────────
        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch (IOException ex)
        {
            return FileValidationResult.Fail($"Cannot read file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return FileValidationResult.Fail($"Access denied: {ex.Message}");
        }

        // ── 3. Not empty ─────────────────────────────────────────────────────
        // Blank lines only → treat as empty
        var nonBlankLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        if (nonBlankLines.Length == 0)
            return FileValidationResult.Fail("The file is empty.");

        // ── 4. Minimum column count in first data-looking row ─────────────────
        // A valid data row needs at least 4 comma-separated fields.
        // We check the first non-blank line here purely for a sanity guard;
        // actual per-row validation happens in the parser.
        var firstLine = nonBlankLines[0];
        var firstFields = SplitCsvLine(firstLine);
        if (firstFields.Length < 4)
        {
            // Could still be a header with fewer columns — but data will never be valid.
            return FileValidationResult.Fail(
                $"File does not appear to be a valid employee CSV. " +
                $"Expected at least 4 columns but found {firstFields.Length} in the first row.");
        }

        // ── 5. Header detection ───────────────────────────────────────────────
        // A row is a header if its first column is:
        //   (a) Not parseable as an integer, OR
        //   (b) A known header keyword (case-insensitive).
        var firstColumnValue = firstFields[0].Trim();
        bool hasHeader = !int.TryParse(firstColumnValue, out _)
                         || HeaderKeywords.Contains(firstColumnValue);

        return FileValidationResult.Ok(hasHeader);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Splits a CSV line by comma and trims each field.
    private static string[] SplitCsvLine(string line)
        => line.Split(',').Select(f => f.Trim()).ToArray();
}
