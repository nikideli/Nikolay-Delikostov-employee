using EmployeePairFinder.Models;

namespace EmployeePairFinder.Services;

public interface IEmployeePairService
{
    // Returns per-project rows for the pair(s) with the highest total collaboration days.
    // All tied pairs are included. Returns empty list when fewer than 2 employees exist.
    IReadOnlyList<EmployeePairResult> FindLongestCollaboratingPair(IReadOnlyList<EmployeeRecord> records);
}
