using EmployeePairFinder.Enums;
using EmployeePairFinder.Models;

namespace EmployeePairFinder.Services;

public interface ICsvParserService
{
    // Returns successfully parsed records plus any non-fatal row-level warnings.
    CsvParseResult Parse(string filePath, DateFormatPreference preference);
}
