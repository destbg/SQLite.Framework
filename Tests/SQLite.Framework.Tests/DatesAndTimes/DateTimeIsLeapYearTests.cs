using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class DilyRow
{
    [Key]
    public int Id { get; set; }

    public DateTime Value { get; set; }

    public DateTimeOffset Offset { get; set; }

    public DateOnly Date { get; set; }

    public int StoredYear { get; set; }
}

internal static class DilyCalendar
{
    public static int Year => 2000;
}

public class DateTimeIsLeapYearTests
{
    private static readonly DilyRow[] Rows =
    [
        CreateRow(1, 1),
        CreateRow(2, 4),
        CreateRow(3, 100),
        CreateRow(4, 400),
        CreateRow(5, 1900),
        CreateRow(6, 2000),
        CreateRow(7, 2004),
        CreateRow(8, 2100),
        CreateRow(9, 2400),
        CreateRow(10, 9999)
    ];

    [Fact]
    public void IsLeapYearInFilterMatchesObjects()
    {
        using TestDatabase db = Seed();
        List<int> expected = Rows.Where(x => DateTime.IsLeapYear(x.Value.Year)).Select(x => x.Id).ToList();
        IQueryable<int> query = db.Table<DilyRow>().Where(x => DateTime.IsLeapYear(x.Value.Year)).OrderBy(x => x.Id).Select(x => x.Id);

        Assert.Equal(expected, query.ToList());

        SQLiteCommand command = query.ToSqlCommand();
        Assert.Equal("SELECT d0.\"Id\" AS \"Id\"\nFROM \"DilyRow\" AS d0\nWHERE (STRFTIME('%j', PRINTF('%04d-12-31', CAST(STRFTIME('%Y',DATETIME((d0.\"Value\" - @p0) / @p1 - (CASE WHEN ((d0.\"Value\" - @p0) % @p1) < 0 THEN 1 ELSE 0 END), 'unixepoch')) AS INTEGER))) = '366')\nORDER BY d0.\"Id\" ASC", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void IsLeapYearInProjectionMatchesObjects()
    {
        using TestDatabase db = Seed();
        List<bool> expected = Rows.OrderBy(x => x.Id).Select(x => DateTime.IsLeapYear(x.Value.Year)).ToList();
        List<bool> actual = db.Table<DilyRow>().OrderBy(x => x.Id).Select(x => DateTime.IsLeapYear(x.Value.Year)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsLeapYearAfterDateArithmeticMatchesObjects()
    {
        using TestDatabase db = Seed();
        List<int> expected = Rows.Where(x => x.Value.Year < 9999 && DateTime.IsLeapYear(x.Value.AddYears(1).Year)).Select(x => x.Id).ToList();
        List<int> actual = db.Table<DilyRow>().Where(x => x.Value.Year < 9999 && DateTime.IsLeapYear(x.Value.AddYears(1).Year)).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsLeapYearFromDateTimeOffsetMatchesObjects()
    {
        using TestDatabase db = Seed();
        List<int> expected = Rows.Where(x => DateTime.IsLeapYear(x.Offset.Year)).Select(x => x.Id).ToList();
        List<int> actual = db.Table<DilyRow>().Where(x => DateTime.IsLeapYear(x.Offset.Year)).OrderBy(x => x.Id).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsLeapYearFromDateOnlyMatchesObjects()
    {
        using TestDatabase db = Seed();
        List<int> expected = Rows.Where(x => DateTime.IsLeapYear(x.Date.Year)).Select(x => x.Id).ToList();
        List<int> actual = db.Table<DilyRow>().Where(x => DateTime.IsLeapYear(x.Date.Year)).OrderBy(x => x.Id).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsLeapYearOverStoredIntegerMatchesObjects()
    {
        using TestDatabase db = Seed();
        List<int> expected = Rows.Where(x => DateTime.IsLeapYear(x.StoredYear)).Select(x => x.Id).ToList();
        List<int> actual = db.Table<DilyRow>().Where(x => DateTime.IsLeapYear(x.StoredYear)).OrderBy(x => x.Id).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsLeapYearOverStaticYearMemberMatchesObjects()
    {
        using TestDatabase db = Seed();
        List<int> expected = Rows.Where(_ => DateTime.IsLeapYear(DilyCalendar.Year)).Select(x => x.Id).ToList();
        List<int> actual = db.Table<DilyRow>().Where(_ => DateTime.IsLeapYear(DilyCalendar.Year)).OrderBy(x => x.Id).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsLeapYearOverComputedIntegerMatchesObjects()
    {
        using TestDatabase db = Seed();
        List<int> expected = Rows.Where(x => DateTime.IsLeapYear(x.StoredYear + 0)).Select(x => x.Id).ToList();
        List<int> actual = db.Table<DilyRow>().Where(x => DateTime.IsLeapYear(x.StoredYear + 0)).OrderBy(x => x.Id).Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsLeapYearOutsideTheDotNetRangeUsesTheSQLiteResult()
    {
        using TestDatabase db = Seed();
        DilyRow[] invalidRows =
        [
            CreateRow(11, 2001, 0),
            CreateRow(12, 2001, -1),
            CreateRow(13, 2001, 10000)
        ];
        db.Table<DilyRow>().AddRange(invalidRows);

        Assert.Throws<ArgumentOutOfRangeException>(() => invalidRows
            .Select(x => DateTime.IsLeapYear(x.StoredYear))
            .ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() => db.Table<DilyRow>()
            .Where(x => x.Id >= 11)
            .OrderBy(x => x.Id)
            .Select(x => DateTime.IsLeapYear(x.StoredYear))
            .ToList());

        List<int> actual = db.Table<DilyRow>()
            .Where(x => x.Id >= 11 && DateTime.IsLeapYear(x.StoredYear))
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([11], actual);
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<DilyRow>().Schema.CreateTable();
        db.Table<DilyRow>().AddRange(Rows);
        return db;
    }

    private static DilyRow CreateRow(int id, int year, int? storedYear = null)
    {
        return new DilyRow
        {
            Id = id,
            Value = new DateTime(year, 6, 1),
            Offset = new DateTimeOffset(year, 6, 1, 0, 0, 0, TimeSpan.Zero),
            Date = new DateOnly(year, 6, 1),
            StoredYear = storedYear ?? year
        };
    }
}
