namespace EmployeePairFinder.Models;

public sealed class CsvParseResult
{
    public IReadOnlyList<EmployeeRecord> Records { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
