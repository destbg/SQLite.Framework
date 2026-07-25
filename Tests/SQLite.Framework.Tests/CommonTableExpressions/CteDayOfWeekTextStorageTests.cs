using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("HsjCteDowRows")]
public class HsjCteDowRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

public class HsjCteDowValue
{
    public HsjCteDowValue(int Id, DayOfWeek D)
    {
        this.Id = Id;
        this.D = D;
    }

    public int Id { get; }

    public DayOfWeek D { get; }
}

public class CteDayOfWeekTextStorageTests
{
    private static List<HsjCteDowRow> Rows()
    {
        return
        [
            new HsjCteDowRow { Id = 1, When = new DateTime(2024, 1, 1, 8, 0, 0) },
            new HsjCteDowRow { Id = 2, When = new DateTime(2024, 1, 2, 8, 0, 0) },
            new HsjCteDowRow { Id = 3, When = new DateTime(2024, 1, 7, 8, 0, 0) },
            new HsjCteDowRow { Id = 4, When = new DateTime(2024, 1, 8, 8, 0, 0) }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<HsjCteDowRow>().Schema.CreateTable();
        db.Table<HsjCteDowRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void FilterByDayOfWeekConstant()
    {
        using TestDatabase db = Setup();
        List<HsjCteDowRow> local = Rows();

        SQLiteCte<HsjCteDowValue> cte = db.With<HsjCteDowValue>(() =>
            db.Table<HsjCteDowRow>().Select(x => new HsjCteDowValue(x.Id, x.When.DayOfWeek)));

        List<int> expected = local
            .Select(x => new HsjCteDowValue(x.Id, x.When.DayOfWeek))
            .Where(c => c.D == DayOfWeek.Monday)
            .Select(c => c.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = cte
            .Where(c => c.D == DayOfWeek.Monday)
            .Select(c => c.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectDayOfWeekEquality()
    {
        using TestDatabase db = Setup();
        List<HsjCteDowRow> local = Rows();

        SQLiteCte<HsjCteDowValue> cte = db.With<HsjCteDowValue>(() =>
            db.Table<HsjCteDowRow>().Select(x => new HsjCteDowValue(x.Id, x.When.DayOfWeek)));

        List<bool> expected = local
            .Select(x => new HsjCteDowValue(x.Id, x.When.DayOfWeek))
            .OrderBy(c => c.Id)
            .Select(c => c.D == DayOfWeek.Monday)
            .ToList();

        List<bool> actual = cte
            .OrderBy(c => c.Id)
            .Select(c => c.D == DayOfWeek.Monday)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
