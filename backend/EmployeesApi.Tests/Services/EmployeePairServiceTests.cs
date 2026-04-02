using EmployeesApi.Models;
using EmployeesApi.Services;
using NUnit.Framework;

namespace EmployeesApi.Tests.Services;

[TestFixture]
public class EmployeePairServiceTests
{
    private EmployeePairService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new EmployeePairService();
    }

    private static EmployeeRecord CreateRecord(int empId, int projectId, string from, string to) =>
        new()
        {
            EmpID     = empId,
            ProjectID = projectId,
            DateFrom  = DateTime.Parse(from),
            DateTo    = DateTime.Parse(to)
        };

    [Test]
    public void FindLongestCollaboratingPair_ReturnsEmpty_WhenNoRecords()
    {
        var result = _service.FindLongestCollaboratingPair([]);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FindLongestCollaboratingPair_ReturnsEmpty_WhenOnlyOneDistinctEmployee()
    {
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-06-30"),
            CreateRecord(1, 10, "2020-07-01", "2020-12-31"),
        };

        var result = _service.FindLongestCollaboratingPair(records);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FindLongestCollaboratingPair_ReturnsEmpty_WhenNoPairHasDateOverlap()
    {
        // E1: Jan–Mar, E2: Jul–Dec — no date overlap
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-03-31"),
            CreateRecord(2, 10, "2020-07-01", "2020-12-31"),
        };

        var result = _service.FindLongestCollaboratingPair(records);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FindLongestCollaboratingPair_ReturnsEmpty_WhenRangesAreAdjacentButNotOverlapping()
    {
        // E1 ends exactly on the day E2 starts → overlapEnd == overlapStart → 0 days
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-06-01"),
            CreateRecord(2, 10, "2020-06-01", "2020-12-31"),
        };

        var result = _service.FindLongestCollaboratingPair(records);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FindLongestCollaboratingPair_ReturnsEmpty_WhenEmployeesShareNoCommonProject()
    {
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-12-31"),
            CreateRecord(2, 20, "2020-01-01", "2020-12-31"),
        };

        var result = _service.FindLongestCollaboratingPair(records);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FindLongestCollaboratingPair_ReturnsEmpty_WhenAllRowsBelongToOneEmployee()
    {
        // Only one distinct employee ID; no valid pair should be found
        var records = new List<EmployeeRecord>
        {
            CreateRecord(5, 10, "2020-01-01", "2020-06-30"),
            CreateRecord(5, 10, "2020-07-01", "2020-12-31"),
        };

        var result = _service.FindLongestCollaboratingPair(records);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FindLongestCollaboratingPair_ReturnsCorrectDaysWorked_WhenTwoEmployeesShareOneProject()
    {
        // E1: Jan 1 – Dec 31, E2: Jul 1 – Dec 31 → overlap Jul 1 – Dec 31
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-12-31"),
            CreateRecord(2, 10, "2020-07-01", "2020-12-31"),
        };

        var result = _service.FindLongestCollaboratingPair(records);

        Assert.That(result, Has.Count.EqualTo(1));
        var row = result[0];
        int expectedDays = (DateTime.Parse("2020-12-31") - DateTime.Parse("2020-07-01")).Days; // 183
        Assert.Multiple(() =>
        {
            Assert.That(row.Employee1Id, Is.EqualTo(1));
            Assert.That(row.Employee2Id, Is.EqualTo(2));
            Assert.That(row.ProjectId,   Is.EqualTo(10));
            Assert.That(row.DaysWorked,  Is.EqualTo(expectedDays));
        });
    }

    [Test]
    public void FindLongestCollaboratingPair_ReturnsPartialOverlapDays_WhenRangesPartiallyOverlap()
    {
        // E1: Jan 1 – Apr 30, E2: Mar 1 – Jun 30 → overlap Mar 1 – Apr 30
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-04-30"),
            CreateRecord(2, 10, "2020-03-01", "2020-06-30"),
        };

        int expected = (DateTime.Parse("2020-04-30") - DateTime.Parse("2020-03-01")).Days; // 60

        var result = _service.FindLongestCollaboratingPair(records);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].DaysWorked, Is.EqualTo(expected));
    }

    [Test]
    public void FindLongestCollaboratingPair_ReturnsInnerRangeDuration_WhenOneEmployeeRangeContainsTheOther()
    {
        // E2's range is fully inside E1's range → overlap equals E2's duration
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-12-31"),
            CreateRecord(2, 10, "2020-03-01", "2020-09-30"),
        };

        int expected = (DateTime.Parse("2020-09-30") - DateTime.Parse("2020-03-01")).Days; // 213

        var result = _service.FindLongestCollaboratingPair(records);

        Assert.That(result[0].DaysWorked, Is.EqualTo(expected));
    }

    [Test]
    public void FindLongestCollaboratingPair_NormalisesEmployeeIdOrder_WhenHigherIdIsPassedFirst()
    {
        // Pass higher ID first to verify the pair is normalised so Emp1 < Emp2
        var records = new List<EmployeeRecord>
        {
            CreateRecord(99, 10, "2020-01-01", "2020-12-31"),
            CreateRecord(1,  10, "2020-01-01", "2020-12-31"),
        };

        var result = _service.FindLongestCollaboratingPair(records);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Employee1Id, Is.LessThan(result[0].Employee2Id));
        Assert.That(result[0].Employee1Id, Is.EqualTo(1));
        Assert.That(result[0].Employee2Id, Is.EqualTo(99));
    }

    [Test]
    public void FindLongestCollaboratingPair_AccumulatesDaysAcrossAllSharedProjects_WhenPairWorksOnMultipleProjects()
    {
        // E1+E2 share two projects; total should be the sum of both overlaps
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-06-30"),
            CreateRecord(2, 10, "2020-01-01", "2020-06-30"),
            CreateRecord(1, 20, "2021-01-01", "2021-03-31"),
            CreateRecord(2, 20, "2021-01-01", "2021-03-31"),
        };

        var result = _service.FindLongestCollaboratingPair(records);

        int p10 = (DateTime.Parse("2020-06-30") - DateTime.Parse("2020-01-01")).Days; // 181 (2020 is a leap year)
        int p20 = (DateTime.Parse("2021-03-31") - DateTime.Parse("2021-01-01")).Days; // 89
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Sum(r => r.DaysWorked), Is.EqualTo(p10 + p20));
    }

    [Test]
    public void FindLongestCollaboratingPair_SelectsWinnerByTotalDaysNotSingleProject_WhenMultiplePairsExist()
    {
        // Pair (1,2) wins one big project; pair (1,3) wins two smaller ones whose sum > (1,2)
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-06-30"),
            CreateRecord(2, 10, "2020-01-01", "2020-06-30"),
            CreateRecord(1, 20, "2021-01-01", "2021-04-10"),
            CreateRecord(3, 20, "2021-01-01", "2021-04-10"),
            CreateRecord(1, 30, "2022-01-01", "2022-04-10"),
            CreateRecord(3, 30, "2022-01-01", "2022-04-10"),
        };

        var result = _service.FindLongestCollaboratingPair(records);

        int p20days = (DateTime.Parse("2021-04-10") - DateTime.Parse("2021-01-01")).Days; // 99
        int p30days = (DateTime.Parse("2022-04-10") - DateTime.Parse("2022-01-01")).Days; // 99
        int expectedTotal13 = p20days + p30days;

        Assert.That(expectedTotal13, Is.GreaterThan(180),
            "Pre-condition: pair (1,3) total must beat pair (1,2) for this test to be meaningful");

        Assert.That(result.All(r => r.Employee1Id == 1 && r.Employee2Id == 3));
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void FindLongestCollaboratingPair_ReturnsResultsOrderedByProjectId_WhenWinnerSpansMultipleProjects()
    {
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 30, "2020-01-01", "2020-12-31"),
            CreateRecord(2, 30, "2020-01-01", "2020-12-31"),
            CreateRecord(1, 10, "2021-01-01", "2021-12-31"),
            CreateRecord(2, 10, "2021-01-01", "2021-12-31"),
            CreateRecord(1, 20, "2022-01-01", "2022-12-31"),
            CreateRecord(2, 20, "2022-01-01", "2022-12-31"),
        };

        var result = _service.FindLongestCollaboratingPair(records);

        var projectIds = result.Select(r => r.ProjectId).ToList();
        Assert.That(projectIds, Is.Ordered.Ascending);
    }

    [Test]
    public void FindLongestCollaboratingPair_ReturnsCorrectWinningPair_WhenThreeEmployeesCompete()
    {
        // E1+E2: 30 days; E2+E3: ~200 days → winner is (2,3)
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-01-31"),
            CreateRecord(2, 10, "2020-01-01", "2020-01-31"),
            CreateRecord(2, 20, "2021-01-01", "2021-07-20"),
            CreateRecord(3, 20, "2021-01-01", "2021-07-20"),
        };

        var result = _service.FindLongestCollaboratingPair(records);

        Assert.That(result.All(r => r.Employee1Id == 2 && r.Employee2Id == 3));
    }

    [Test]
    public void FindLongestCollaboratingPair_SumsAllNonOverlappingPeriods_WhenEmployeeHasMultipleRowsForSameProject()
    {
        // E1 has two non-overlapping periods on project 10; each overlaps with E2.
        // Expected total = overlap1 + overlap2 (no double-count).
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-03-31"),
            CreateRecord(1, 10, "2020-07-01", "2020-09-30"), // gap in between
            CreateRecord(2, 10, "2020-01-01", "2020-12-31"), // covers both E1 periods fully
        };

        int expected = (DateTime.Parse("2020-03-31") - DateTime.Parse("2020-01-01")).Days
                     + (DateTime.Parse("2020-09-30") - DateTime.Parse("2020-07-01")).Days;

        var result = _service.FindLongestCollaboratingPair(records);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].DaysWorked, Is.EqualTo(expected));
    }

    [Test]
    public void FindLongestCollaboratingPair_DoesNotDoubleCount_WhenEmployeeHasOverlappingRowsForSameProject()
    {
        // E1 has TWO overlapping rows for project 10:
        //   row 1: Jan 1 – Jun 30
        //   row 2: Apr 1 – Dec 31  (overlaps row 1 by Apr–Jun)
        // Merged → Jan 1 – Dec 31.
        // E2: Jan 1 – Dec 31.
        // Correct overlap = (Dec31 - Jan1).Days = 365 days.
        // Bug (pre-fix) would have returned 180 + 274 = 454 days.
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-06-30"),
            CreateRecord(1, 10, "2020-04-01", "2020-12-31"),
            CreateRecord(2, 10, "2020-01-01", "2020-12-31"),
        };

        int expected = (DateTime.Parse("2020-12-31") - DateTime.Parse("2020-01-01")).Days; // 365

        var result = _service.FindLongestCollaboratingPair(records);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].DaysWorked, Is.EqualTo(expected),
            "Overlapping rows for the same employee must be merged before computing overlap " +
            "to avoid double-counting.");
    }

    [Test]
    public void FindLongestCollaboratingPair_ComputesCorrectOverlap_WhenBothEmployeesHaveMultipleRowsForSameProject()
    {
        // E1: Jan–Mar and Jul–Sep (two non-overlapping periods)
        // E2: Feb–Aug (one period crossing both E1 periods)
        // Overlaps:
        //   E1[Jan–Mar] ∩ E2[Feb–Aug] = Feb 1 – Mar 31
        //   E1[Jul–Sep] ∩ E2[Feb–Aug] = Jul 1 – Aug 31
        var records = new List<EmployeeRecord>
        {
            CreateRecord(1, 10, "2020-01-01", "2020-03-31"),
            CreateRecord(1, 10, "2020-07-01", "2020-09-30"),
            CreateRecord(2, 10, "2020-02-01", "2020-08-31"),
        };

        int seg1 = (DateTime.Parse("2020-03-31") - DateTime.Parse("2020-02-01")).Days; // 59
        int seg2 = (DateTime.Parse("2020-08-31") - DateTime.Parse("2020-07-01")).Days; // 61
        int expected = seg1 + seg2;

        var result = _service.FindLongestCollaboratingPair(records);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].DaysWorked, Is.EqualTo(expected));
    }

    [Test]
    public void MergeIntervals_ReturnsEmpty_WhenInputIsEmpty()
    {
        var result = EmployeePairService.MergeIntervals([]);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void MergeIntervals_ReturnsSameInterval_WhenInputHasOneEntry()
    {
        var interval = (new DateTime(2020, 1, 1), new DateTime(2020, 6, 30));
        var result = EmployeePairService.MergeIntervals([interval]);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(interval));
    }

    [Test]
    public void MergeIntervals_ReturnsBothIntervalsSortedByStart_WhenIntervalsDoNotOverlap()
    {
        var intervals = new List<(DateTime, DateTime)>
        {
            (new DateTime(2020, 7, 1), new DateTime(2020, 12, 31)),
            (new DateTime(2020, 1, 1), new DateTime(2020, 3, 31)),
        };

        var result = EmployeePairService.MergeIntervals(intervals);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Start, Is.EqualTo(new DateTime(2020, 1, 1)));
        Assert.That(result[1].Start, Is.EqualTo(new DateTime(2020, 7, 1)));
    }

    [Test]
    public void MergeIntervals_MergesIntoSingleInterval_WhenIntervalsOverlap()
    {
        var intervals = new List<(DateTime, DateTime)>
        {
            (new DateTime(2020, 1, 1),  new DateTime(2020, 6, 30)),
            (new DateTime(2020, 4, 1),  new DateTime(2020, 12, 31)),
        };

        var result = EmployeePairService.MergeIntervals(intervals);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Start, Is.EqualTo(new DateTime(2020, 1,  1)));
        Assert.That(result[0].End,   Is.EqualTo(new DateTime(2020, 12, 31)));
    }

    [Test]
    public void MergeIntervals_MergesIntoSingleInterval_WhenIntervalsAreAdjacent()
    {
        // end of first == start of second → treated as overlapping (curStart <= lastEnd)
        var intervals = new List<(DateTime, DateTime)>
        {
            (new DateTime(2020, 1, 1), new DateTime(2020, 6, 1)),
            (new DateTime(2020, 6, 1), new DateTime(2020, 12, 31)),
        };

        var result = EmployeePairService.MergeIntervals(intervals);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].End, Is.EqualTo(new DateTime(2020, 12, 31)));
    }

    [Test]
    public void MergeIntervals_MergesOnlyOverlappingIntervals_WhenSomeIntervalsOverlapAndSomeDoNot()
    {
        var intervals = new List<(DateTime, DateTime)>
        {
            (new DateTime(2020, 1, 1),  new DateTime(2020, 3, 31)),
            (new DateTime(2020, 2, 1),  new DateTime(2020, 5, 31)), // overlaps first
            (new DateTime(2020, 9, 1),  new DateTime(2020, 12, 31)), // separate
        };

        var result = EmployeePairService.MergeIntervals(intervals);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo((new DateTime(2020, 1, 1), new DateTime(2020, 5, 31))));
        Assert.That(result[1], Is.EqualTo((new DateTime(2020, 9, 1), new DateTime(2020, 12, 31))));
    }

    [Test]
    public void CalculateOverlapDays_ReturnsZero_WhenRangesDoNotOverlap()
    {
        int days = EmployeePairService.CalculateOverlapDays(
            new DateTime(2020, 1, 1), new DateTime(2020, 3, 31),
            new DateTime(2020, 6, 1), new DateTime(2020, 12, 31));

        Assert.That(days, Is.Zero);
    }

    [Test]
    public void CalculateOverlapDays_ReturnsFullDuration_WhenRangesAreIdentical()
    {
        int days = EmployeePairService.CalculateOverlapDays(
            new DateTime(2020, 1, 1), new DateTime(2020, 12, 31),
            new DateTime(2020, 1, 1), new DateTime(2020, 12, 31));

        Assert.That(days, Is.EqualTo((new DateTime(2020, 12, 31) - new DateTime(2020, 1, 1)).Days));
    }

    [Test]
    public void CalculateOverlapDays_ReturnsIntersectionLength_WhenRangesPartiallyOverlap()
    {
        int days = EmployeePairService.CalculateOverlapDays(
            new DateTime(2020, 1, 1), new DateTime(2020, 6, 30),
            new DateTime(2020, 4, 1), new DateTime(2020, 12, 31));

        int expected = (new DateTime(2020, 6, 30) - new DateTime(2020, 4, 1)).Days;
        Assert.That(days, Is.EqualTo(expected));
    }

    [Test]
    public void CalculateOverlapDays_ReturnsZero_WhenRangesTouchAtSinglePoint()
    {
        // overlapEnd == overlapStart → 0 per current ≤ check
        int days = EmployeePairService.CalculateOverlapDays(
            new DateTime(2020, 1, 1), new DateTime(2020, 6, 1),
            new DateTime(2020, 6, 1), new DateTime(2020, 12, 31));

        Assert.That(days, Is.Zero);
    }
}
