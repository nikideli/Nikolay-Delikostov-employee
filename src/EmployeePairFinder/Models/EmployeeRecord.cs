namespace EmployeePairFinder.Models;

public sealed class EmployeeRecord
{
    public int EmpId { get; init; }
    public int ProjectId { get; init; }
    public DateTime DateFrom { get; init; }
    public DateTime? DateTo { get; init; }

    // Resolves null DateTo to today — used in all overlap calculations.
    public DateTime EffectiveDateTo => DateTo ?? DateTime.Today;
}
