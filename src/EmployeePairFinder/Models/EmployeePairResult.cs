namespace EmployeePairFinder.Models;

public sealed class EmployeePairResult
{
    public int EmpId1 { get; init; }      // always the lower ID
    public int EmpId2 { get; init; }      // always the higher ID
    public int ProjectId { get; init; }

    // Total calendar days both employees were simultaneously on this project.
    // Inclusive (+1 per overlap) and summed across multiple work periods.
    public int DaysWorked { get; init; }
}
