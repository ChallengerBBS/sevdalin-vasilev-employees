using EmployeesApi.Models;

namespace EmployeesApi.Services;

public class EmployeePairService : IEmployeePairService
{
    public List<EmployeePairResult> FindLongestCollaboratingPair(List<EmployeeRecord> records)
    {
        var pairProjectDays = new Dictionary<(int Emp1, int Emp2, int Project), int>();

        foreach (var projectGroup in records.GroupBy(r => r.ProjectID))
        {
            int projectId = projectGroup.Key;

            // Group by EmpID and merge overlapping/adjacent intervals per employee.
            // This prevents double-counting when an employee has multiple rows for
            // the same project whose date ranges overlap each other.
            var employeeIntervals = projectGroup
                .GroupBy(r => r.EmpID)
                .ToDictionary(
                    g => g.Key,
                    g => MergeIntervals(g.Select(r => (r.DateFrom, r.DateTo)).ToList())
                );

            var employeeIds = employeeIntervals.Keys.ToList();

            for (int i = 0; i < employeeIds.Count; i++)
            {
                for (int j = i + 1; j < employeeIds.Count; j++)
                {
                    int empId1 = employeeIds[i];
                    int empId2 = employeeIds[j];

                    int emp1 = Math.Min(empId1, empId2);
                    int emp2 = Math.Max(empId1, empId2);

                    int totalDays = 0;

                    // Because each employee's intervals are now merged (non-overlapping),
                    // summing all pairwise overlaps between the two sets gives the correct total.
                    foreach (var (start1, end1) in employeeIntervals[empId1])
                    {
                        foreach (var (start2, end2) in employeeIntervals[empId2])
                        {
                            totalDays += CalculateOverlapDays(start1, end1, start2, end2);
                        }
                    }

                    if (totalDays <= 0)
                    {
                        continue;
                    }

                    var key = (emp1, emp2, projectId);
                    pairProjectDays[key] = pairProjectDays.GetValueOrDefault(key) + totalDays;
                }
            }
        }

        if (pairProjectDays.Count == 0)
        {
            return [];
        }

        var best = pairProjectDays
            .GroupBy(kv => (kv.Key.Emp1, kv.Key.Emp2))
            .Select(g => (Pair: g.Key, Total: g.Sum(x => x.Value)))
            .MaxBy(x => x.Total);

        return pairProjectDays
            .Where(kv => kv.Key.Emp1 == best.Pair.Emp1
                      && kv.Key.Emp2 == best.Pair.Emp2)
            .Select(kv => new EmployeePairResult
            {
                Employee1Id = kv.Key.Emp1,
                Employee2Id = kv.Key.Emp2,
                ProjectId   = kv.Key.Project,
                DaysWorked  = kv.Value
            })
            .OrderBy(r => r.ProjectId)
            .ToList();
    }

    /// <summary>
    /// Merges a list of (start, end) date intervals into a minimal set of
    /// non-overlapping intervals sorted by start date.
    /// </summary>
    internal static List<(DateTime Start, DateTime End)> MergeIntervals(
        List<(DateTime Start, DateTime End)> intervals)
    {
        if (intervals.Count <= 1)
        {
            return intervals;
        }

        var sorted = intervals.OrderBy(i => i.Start).ToList();
        var merged = new List<(DateTime Start, DateTime End)> { sorted[0] };

        for (int k = 1; k < sorted.Count; k++)
        {
            var (lastStart, lastEnd) = merged[^1];
            var (curStart, curEnd) = sorted[k];

            if (curStart <= lastEnd)
            {
                // Overlapping or adjacent — extend the last merged interval if needed.
                merged[^1] = (lastStart, curEnd > lastEnd ? curEnd : lastEnd);
            }
            else
            {
                merged.Add((curStart, curEnd));
            }
        }

        return merged;
    }

    internal static int CalculateOverlapDays(
        DateTime start1, DateTime end1,
        DateTime start2, DateTime end2)
    {
        var overlapStart = start1 > start2 ? start1 : start2;
        var overlapEnd = end1 < end2 ? end1 : end2;

        if (overlapEnd <= overlapStart)
        {
            return 0;
        }

        return (overlapEnd - overlapStart).Days;
    }
}
