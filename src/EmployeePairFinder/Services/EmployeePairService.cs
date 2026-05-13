using EmployeePairFinder.Models;

namespace EmployeePairFinder.Services;

// Finds the employee pair(s) with the most total calendar days of simultaneous
// work across shared projects.
//
// Algorithm:
//   1. Group records by (EmpID, ProjectID) to collect all work periods per assignment.
//   2. For every unique pair (A < B), find shared projects and cross all A-periods
//      with all B-periods: overlap = max(0, min(toA,toB) - max(fromA,fromB) + 1).
//      The +1 makes the range inclusive (working on the start day counts as 1).
//   3. Sum overlaps per project and across projects for a total score.
//   4. Return all project-level rows for every pair tied at the maximum.
public sealed class EmployeePairService : IEmployeePairService
{
    /// <inheritdoc/>
    public IReadOnlyList<EmployeePairResult> FindLongestCollaboratingPair(
        IReadOnlyList<EmployeeRecord> records)
    {
        var distinctEmployees = records.Select(r => r.EmpId).Distinct().OrderBy(id => id).ToList();
        if (distinctEmployees.Count < 2)
            return [];

        // ── Step 1: Build period map ───────────────────────────────────────────
        // Key: (EmpID, ProjectID) → Value: list of (From, To) date ranges
        var periodMap = records
            .GroupBy(r => (r.EmpId, r.ProjectId))
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => (From: r.DateFrom, To: r.EffectiveDateTo)).ToList()
            );

        // Pre-build empId → set of projectIds so we never re-scan periodMap.Keys inside the pair loop.
        // Without this, building projectsA/projectsB for each pair is O(E×P) per pair → O(E³P) total.
        var empToProjects = new Dictionary<int, HashSet<int>>();
        foreach (var key in periodMap.Keys)
        {
            if (!empToProjects.TryGetValue(key.EmpId, out var set))
                empToProjects[key.EmpId] = set = new HashSet<int>();
            set.Add(key.ProjectId);
        }

        // ── Step 2: Evaluate all unique pairs ─────────────────────────────────
        var pairProjectDays = new Dictionary<(int EmpA, int EmpB), Dictionary<int, int>>();
        // Track running totals so Step 4 never calls .Sum() again.
        var pairTotals = new Dictionary<(int EmpA, int EmpB), int>();

        for (int i = 0; i < distinctEmployees.Count - 1; i++)
        {
            for (int j = i + 1; j < distinctEmployees.Count; j++)
            {
                int empA = distinctEmployees[i]; // always the lower ID
                int empB = distinctEmployees[j]; // always the higher ID

                if (!empToProjects.TryGetValue(empA, out var projectsA)) continue;
                if (!empToProjects.TryGetValue(empB, out var projectsB)) continue;

                // Iterate A's projects; skip those B didn't work on — no extra HashSet allocated.
                foreach (int projectId in projectsA)
                {
                    if (!projectsB.Contains(projectId)) continue;

                    var periodsA = periodMap[(empA, projectId)];
                    var periodsB = periodMap[(empB, projectId)];

                    // Cross-product of all period combinations for this project.
                    // Handles employees who left and rejoined (multiple rows per project).
                    int daysOnProject = 0;
                    foreach (var pA in periodsA)
                    {
                        foreach (var pB in periodsB)
                        {
                            var overlapStart = pA.From > pB.From ? pA.From : pB.From;
                            var overlapEnd   = pA.To   < pB.To   ? pA.To   : pB.To;

                            // .Days returns int directly — no float arithmetic, no cast.
                            if (overlapStart <= overlapEnd)
                                daysOnProject += (overlapEnd - overlapStart).Days + 1;
                        }
                    }

                    if (daysOnProject <= 0) continue;

                    var pairKey = (empA, empB);
                    if (!pairProjectDays.TryGetValue(pairKey, out var projectDict))
                        pairProjectDays[pairKey] = projectDict = new Dictionary<int, int>();

                    projectDict[projectId] = daysOnProject;
                    pairTotals[pairKey] = pairTotals.TryGetValue(pairKey, out int prev)
                        ? prev + daysOnProject
                        : daysOnProject;
                }
            }
        }

        if (pairProjectDays.Count == 0)
            return [];

        // ── Step 3: Find maximum — single pass over pre-computed int totals ────
        int maxTotal = 0;
        foreach (var t in pairTotals.Values)
            if (t > maxTotal) maxTotal = t;

        // ── Step 4: Collect result rows for every pair tied at the maximum ─────
        var results = new List<EmployeePairResult>();

        foreach (var (pairKey, projectDays) in pairProjectDays)
        {
            if (pairTotals[pairKey] != maxTotal) continue;

            foreach (var (projectId, days) in projectDays.OrderBy(kv => kv.Key))
            {
                results.Add(new EmployeePairResult
                {
                    EmpId1     = pairKey.EmpA,
                    EmpId2     = pairKey.EmpB,
                    ProjectId  = projectId,
                    DaysWorked = days
                });
            }
        }

        return results
            .OrderBy(r => r.EmpId1)
            .ThenBy(r => r.EmpId2)
            .ThenBy(r => r.ProjectId)
            .ToList()
            .AsReadOnly();
    }
}
