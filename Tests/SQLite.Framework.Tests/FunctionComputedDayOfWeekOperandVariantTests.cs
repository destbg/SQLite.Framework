using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FnDowVariantRows")]
public class FnDowVariantRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public DateTime? MaybeWhen { get; set; }
}

public class FunctionComputedDayOfWeekOperandVariantTests
{
    private static TestDatabase CreateSeeded(out List<FnDowVariantRow> rows)
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<FnDowVariantRow>().Schema.CreateTable();
        rows =
        [
            new FnDowVariantRow { Id = 1, When = new DateTime(2024, 1, 1), MaybeWhen = new DateTime(2024, 1, 1) },
            new FnDowVariantRow { Id = 2, When = new DateTime(2024, 1, 2), MaybeWhen = null },
            new FnDowVariantRow { Id = 3, When = new DateTime(2024, 1, 3), MaybeWhen = new DateTime(2024, 1, 7) },
            new FnDowVariantRow { Id = 4, When = new DateTime(2024, 1, 4), MaybeWhen = null },
        ];
        db.Table<FnDowVariantRow>().AddRange(rows);
        return db;
    }

    [Fact]
    public void Iif_ComputedDayOfWeekBranch_MatchesLinq()
    {
        using TestDatabase db = CreateSeeded(out List<FnDowVariantRow> rows);

        List<DayOfWeek> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => r.Id % 2 == 0 ? r.When.DayOfWeek : DayOfWeek.Friday)
            .ToList();

        List<DayOfWeek> actual = db.Table<FnDowVariantRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteFunctions.Iif(r.Id % 2 == 0, r.When.DayOfWeek, DayOfWeek.Friday))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Coalesce_ComputedDayOfWeekWithConstant_MatchesLinq()
    {
        using TestDatabase db = CreateSeeded(out List<FnDowVariantRow> rows);

        List<DayOfWeek?> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => (DayOfWeek?)(r.MaybeWhen == null ? DayOfWeek.Friday : r.MaybeWhen.Value.DayOfWeek))
            .ToList();

        List<DayOfWeek?> actual = db.Table<FnDowVariantRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteFunctions.Coalesce((DayOfWeek?)r.MaybeWhen!.Value.DayOfWeek, DayOfWeek.Friday))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctFrom_ComputedDayOfWeek_MatchesLinq()
    {
        using TestDatabase db = CreateSeeded(out List<FnDowVariantRow> rows);

        List<bool> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => r.When.DayOfWeek != DayOfWeek.Monday)
            .ToList();

        List<bool> actual = db.Table<FnDowVariantRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteFunctions.DistinctFrom(r.When.DayOfWeek, DayOfWeek.Monday))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void In_ComputedDayOfWeekItem_MatchesLinq()
    {
        using TestDatabase db = CreateSeeded(out List<FnDowVariantRow> rows);

        List<int> expected = rows
            .Where(r => DayOfWeek.Monday == r.When.DayOfWeek || DayOfWeek.Monday == DayOfWeek.Wednesday)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<FnDowVariantRow>()
            .Where(r => SQLiteFunctions.In(DayOfWeek.Monday, r.When.DayOfWeek, DayOfWeek.Wednesday))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Between_ComputedDayOfWeekBound_MatchesLinq()
    {
        using TestDatabase db = CreateSeeded(out List<FnDowVariantRow> rows);

        List<int> expected = rows
            .Where(r => r.When.DayOfWeek <= DayOfWeek.Tuesday && DayOfWeek.Tuesday <= DayOfWeek.Saturday)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<FnDowVariantRow>()
            .Where(r => SQLiteFunctions.Between(DayOfWeek.Tuesday, r.When.DayOfWeek, DayOfWeek.Saturday))
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Nullif_ConstantFirstComputedSecond_MatchesLinq()
    {
        using TestDatabase db = CreateSeeded(out List<FnDowVariantRow> rows);

        List<DayOfWeek?> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => r.When.DayOfWeek == DayOfWeek.Monday ? (DayOfWeek?)null : DayOfWeek.Monday)
            .ToList();

        List<DayOfWeek?> actual = db.Table<FnDowVariantRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteFunctions.Nullif((DayOfWeek?)DayOfWeek.Monday, r.When.DayOfWeek))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
