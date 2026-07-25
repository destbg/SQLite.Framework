using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FnDayOfWeekRows")]
public class FnDayOfWeekRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

public class FunctionComputedDayOfWeekTextStorageTests
{
    private static TestDatabase CreateSeeded(out List<FnDayOfWeekRow> rows)
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<FnDayOfWeekRow>().Schema.CreateTable();
        rows =
        [
            new FnDayOfWeekRow { Id = 1, When = new DateTime(2024, 1, 1) },
            new FnDayOfWeekRow { Id = 2, When = new DateTime(2024, 1, 2) },
            new FnDayOfWeekRow { Id = 3, When = new DateTime(2024, 1, 3) },
            new FnDayOfWeekRow { Id = 4, When = new DateTime(2024, 1, 4) },
        ];
        db.Table<FnDayOfWeekRow>().AddRange(rows);
        return db;
    }

    [Fact]
    public void EqualityOnComputedDayOfWeek_MatchesLinq()
    {
        using TestDatabase db = CreateSeeded(out List<FnDayOfWeekRow> rows);

        List<int> expected = rows
            .Where(r => r.When.DayOfWeek == DayOfWeek.Monday)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<FnDayOfWeekRow>()
            .Where(r => r.When.DayOfWeek == DayOfWeek.Monday)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void In_ComputedDayOfWeek_MatchesLinq()
    {
        using TestDatabase db = CreateSeeded(out List<FnDayOfWeekRow> rows);

        List<int> expected = rows
            .Where(r => new[] { DayOfWeek.Monday, DayOfWeek.Wednesday }.Contains(r.When.DayOfWeek))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<FnDayOfWeekRow>()
            .Where(r => SQLiteFunctions.In(r.When.DayOfWeek, DayOfWeek.Monday, DayOfWeek.Wednesday))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Between_ComputedDayOfWeek_MatchesLinq()
    {
        using TestDatabase db = CreateSeeded(out List<FnDayOfWeekRow> rows);

        List<int> expected = rows
            .Where(r => r.When.DayOfWeek >= DayOfWeek.Monday && r.When.DayOfWeek <= DayOfWeek.Wednesday)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<FnDayOfWeekRow>()
            .Where(r => SQLiteFunctions.Between(r.When.DayOfWeek, DayOfWeek.Monday, DayOfWeek.Wednesday))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Nullif_ComputedDayOfWeek_MatchesLinq()
    {
        using TestDatabase db = CreateSeeded(out List<FnDayOfWeekRow> rows);

        List<DayOfWeek?> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => r.When.DayOfWeek == DayOfWeek.Monday ? (DayOfWeek?)null : r.When.DayOfWeek)
            .ToList();

        List<DayOfWeek?> actual = db.Table<FnDayOfWeekRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteFunctions.Nullif((DayOfWeek?)r.When.DayOfWeek, DayOfWeek.Monday))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Min_ComputedDayOfWeekAndConstant_MatchesLinq()
    {
        using TestDatabase db = CreateSeeded(out List<FnDayOfWeekRow> rows);

        List<DayOfWeek> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => r.When.DayOfWeek < DayOfWeek.Tuesday ? r.When.DayOfWeek : DayOfWeek.Tuesday)
            .ToList();

        List<DayOfWeek> actual = db.Table<FnDayOfWeekRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteFunctions.Min(r.When.DayOfWeek, DayOfWeek.Tuesday))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Max_ComputedDayOfWeekAndConstant_MatchesLinq()
    {
        using TestDatabase db = CreateSeeded(out List<FnDayOfWeekRow> rows);

        List<DayOfWeek> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => r.When.DayOfWeek > DayOfWeek.Tuesday ? r.When.DayOfWeek : DayOfWeek.Tuesday)
            .ToList();

        List<DayOfWeek> actual = db.Table<FnDayOfWeekRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteFunctions.Max(r.When.DayOfWeek, DayOfWeek.Tuesday))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
