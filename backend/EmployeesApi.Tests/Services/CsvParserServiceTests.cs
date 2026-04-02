using System.Text;
using EmployeesApi.Services;
using NUnit.Framework;

namespace EmployeesApi.Tests.Services;

[TestFixture]
public class CsvParserServiceTests
{
    private CsvParserService _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new CsvParserService();
    }

    private static Stream ToStream(string csv) =>
        new MemoryStream(Encoding.UTF8.GetBytes(csv));

    [Test]
    public void Parse_ReturnsEmptyList_WhenCsvIsEmpty()
    {
        var result = _parser.Parse(ToStream(string.Empty));
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Parse_ReturnsEmpty_WhenCsvContainsOnlyBlankLines()
    {
        var result = _parser.Parse(ToStream("\n\n   \n"));
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Parse_ReturnsOneRecord_WhenCsvHasASingleValidRow()
    {
        const string csv = "1,10,2020-01-01,2020-06-30";
        var result = _parser.Parse(ToStream(csv));

        Assert.That(result, Has.Count.EqualTo(1));
        var r = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(r.EmpID,     Is.EqualTo(1));
            Assert.That(r.ProjectID, Is.EqualTo(10));
            Assert.That(r.DateFrom,  Is.EqualTo(new DateTime(2020, 1, 1)));
            Assert.That(r.DateTo,    Is.EqualTo(new DateTime(2020, 6, 30)));
        });
    }

    [Test]
    public void Parse_SkipsHeaderRow_WhenFirstLineIsNonNumeric()
    {
        const string csv = "EmpID,ProjectID,DateFrom,DateTo\n1,10,2020-01-01,2020-06-30";
        var result = _parser.Parse(ToStream(csv));

        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_ReturnsAllRecords_WhenCsvHasMultipleValidRows()
    {
        const string csv =
            "1,10,2020-01-01,2020-06-30\n" +
            "2,10,2020-03-01,2020-09-30\n" +
            "3,20,2021-01-01,2021-12-31";

        var result = _parser.Parse(ToStream(csv));
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void Parse_TreatsDateToAsToday_WhenDateToIsNULL()
    {
        const string csv = "1,10,2020-01-01,NULL";
        var result = _parser.Parse(ToStream(csv));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].DateTo.Date, Is.EqualTo(DateTime.Today));
    }

    [Test]
    public void Parse_TreatsDateToAsToday_WhenDateToIsLowercaseNull()
    {
        const string csv = "1,10,2020-01-01,null";
        var result = _parser.Parse(ToStream(csv));

        Assert.That(result[0].DateTo.Date, Is.EqualTo(DateTime.Today));
    }

    [Test]
    public void Parse_TreatsDateToAsToday_WhenDateToIsEmpty()
    {
        const string csv = "1,10,2020-01-01,";
        var result = _parser.Parse(ToStream(csv));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].DateTo.Date, Is.EqualTo(DateTime.Today));
    }

    [Test]
    public void Parse_SkipsRow_WhenFewerThanFourColumnsArePresent()
    {
        const string csv = "1,10,2020-01-01";
        var result = _parser.Parse(ToStream(csv));
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Parse_SkipsRow_WhenEmpIdIsNonNumeric()
    {
        const string csv = "ABC,10,2020-01-01,2020-06-30";
        var result = _parser.Parse(ToStream(csv));
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Parse_SkipsRow_WhenProjectIdIsNonNumeric()
    {
        const string csv = "1,PROJ,2020-01-01,2020-06-30";
        var result = _parser.Parse(ToStream(csv));
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Parse_SkipsRow_WhenDateFromIsUnparseable()
    {
        const string csv = "1,10,not-a-date,2020-06-30";
        var result = _parser.Parse(ToStream(csv));
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Parse_SkipsRow_WhenDateToIsUnparseable()
    {
        const string csv = "1,10,2020-01-01,not-a-date";
        var result = _parser.Parse(ToStream(csv));
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Parse_ReturnsOnlyValidRecords_WhenCsvContainsMixedValidAndInvalidRows()
    {
        const string csv =
            "1,10,2020-01-01,2020-06-30\n" +   // valid
            "BAD_ROW\n" +                        // invalid
            "2,20,2021-01-01,2021-12-31\n" +    // valid
            "3,30,not-a-date,2021-12-31";        // invalid DateFrom

        var result = _parser.Parse(ToStream(csv));
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].EmpID, Is.EqualTo(1));
        Assert.That(result[1].EmpID, Is.EqualTo(2));
    }

    [Test]
    public void Parse_SkipsBlankLines_WhenBlankLinesAppearBetweenValidRows()
    {
        const string csv =
            "1,10,2020-01-01,2020-06-30\n" +
            "\n" +
            "2,20,2021-01-01,2021-12-31";

        var result = _parser.Parse(ToStream(csv));
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void Parse_UsesFirstFourColumns_WhenRowHasExtraColumns()
    {
        const string csv = "1,10,2020-01-01,2020-06-30,extra,columns";
        var result = _parser.Parse(ToStream(csv));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].EmpID, Is.EqualTo(1));
    }

    [TestCase("2020-01-15",      2020, 1, 15, TestName = "ISO_yyyy-MM-dd")]
    [TestCase("2020/01/15",      2020, 1, 15, TestName = "ISO_yyyy/MM/dd")]
    [TestCase("20200115",        2020, 1, 15, TestName = "Compact_yyyyMMdd")]
    [TestCase("15/01/2020",      2020, 1, 15, TestName = "European_dd/MM/yyyy")]
    [TestCase("15-01-2020",      2020, 1, 15, TestName = "European_dd-MM-yyyy")]
    [TestCase("15.01.2020",      2020, 1, 15, TestName = "European_dd.MM.yyyy")]
    [TestCase("01/15/2020",      2020, 1, 15, TestName = "US_MM/dd/yyyy")]
    [TestCase("15 Jan 2020",     2020, 1, 15, TestName = "LongMonth_dd_MMM_yyyy")]
    [TestCase("January 15 2020", 2020, 1, 15, TestName = "LongMonth_MMMM_dd_yyyy")]
    public void Parse_ParsesDateCorrectly_WhenVariousDateFormatsAreUsed(string dateStr, int year, int month, int day)
    {
        string csv = $"1,10,{dateStr},2020-12-31";
        var result = _parser.Parse(ToStream(csv));

        Assert.That(result, Has.Count.EqualTo(1), $"Failed to parse date: {dateStr}");
        Assert.That(result[0].DateFrom, Is.EqualTo(new DateTime(year, month, day)));
    }

    [Test]
    public void Parse_ParsesCorrectly_WhenValuesHaveLeadingAndTrailingSpaces()
    {
        const string csv = "  1  ,  10  ,  2020-01-01  ,  2020-06-30  ";
        var result = _parser.Parse(ToStream(csv));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].EmpID, Is.EqualTo(1));
    }
}
