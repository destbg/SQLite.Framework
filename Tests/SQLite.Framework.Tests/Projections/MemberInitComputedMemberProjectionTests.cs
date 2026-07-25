using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("HsjMiRows")]
public class HsjMiRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public int? MaybeCount { get; set; }

    public DateTime When { get; set; }
}

public class HsjMiLenDto
{
    public int Id { get; set; }

    public int L { get; set; }
}

public class HsjMiYearDto
{
    public int Id { get; set; }

    public int Y { get; set; }
}

public class HsjMiValueDto
{
    public int Id { get; set; }

    public int C { get; set; }
}

public class HsjMiDowDto
{
    public int Id { get; set; }

    public DayOfWeek D { get; set; }
}

public class HsjMiInner
{
    public DayOfWeek D { get; set; }
}

public class HsjMiOuter
{
    public int Id { get; set; }

    public HsjMiInner? Inner { get; set; }
}

public class MemberInitComputedMemberProjectionTests
{
    private static List<HsjMiRow> Rows()
    {
        return
        [
            new HsjMiRow { Id = 1, Name = "alpha", MaybeCount = 9, When = new DateTime(2024, 1, 1, 8, 0, 0) },
            new HsjMiRow { Id = 2, Name = "be", MaybeCount = 3, When = new DateTime(2024, 1, 2, 8, 0, 0) },
            new HsjMiRow { Id = 3, Name = "gamma", MaybeCount = 4, When = new DateTime(2024, 1, 7, 8, 0, 0) },
            new HsjMiRow { Id = 4, Name = "d", MaybeCount = 6, When = new DateTime(2024, 1, 8, 8, 0, 0) }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<HsjMiRow>().Schema.CreateTable();
        db.Table<HsjMiRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void StringLengthMember()
    {
        using TestDatabase db = Setup();
        List<HsjMiRow> local = Rows();

        List<int> expected = local
            .OrderBy(x => x.Id)
            .Select(x => new HsjMiLenDto { Id = x.Id, L = x.Name.Length })
            .Select(c => c.L)
            .ToList();

        List<int> actual = db.Table<HsjMiRow>()
            .OrderBy(x => x.Id)
            .Select(x => new HsjMiLenDto { Id = x.Id, L = x.Name.Length })
            .ToList()
            .Select(c => c.L)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DateYearMember()
    {
        using TestDatabase db = Setup();
        List<HsjMiRow> local = Rows();

        List<int> expected = local
            .OrderBy(x => x.Id)
            .Select(x => new HsjMiYearDto { Id = x.Id, Y = x.When.Year })
            .Select(c => c.Y)
            .ToList();

        List<int> actual = db.Table<HsjMiRow>()
            .OrderBy(x => x.Id)
            .Select(x => new HsjMiYearDto { Id = x.Id, Y = x.When.Year })
            .ToList()
            .Select(c => c.Y)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableValueMember()
    {
        using TestDatabase db = Setup();
        List<HsjMiRow> local = Rows();

        List<int> expected = local
            .Where(x => x.MaybeCount != null)
            .OrderBy(x => x.Id)
            .Select(x => new HsjMiValueDto { Id = x.Id, C = x.MaybeCount!.Value })
            .Select(c => c.C)
            .ToList();

        List<int> actual = db.Table<HsjMiRow>()
            .Where(x => x.MaybeCount != null)
            .OrderBy(x => x.Id)
            .Select(x => new HsjMiValueDto { Id = x.Id, C = x.MaybeCount!.Value })
            .ToList()
            .Select(c => c.C)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DayOfWeekMember()
    {
        using TestDatabase db = Setup();
        List<HsjMiRow> local = Rows();

        List<DayOfWeek> expected = local
            .OrderBy(x => x.Id)
            .Select(x => new HsjMiDowDto { Id = x.Id, D = x.When.DayOfWeek })
            .Select(c => c.D)
            .ToList();

        List<DayOfWeek> actual = db.Table<HsjMiRow>()
            .OrderBy(x => x.Id)
            .Select(x => new HsjMiDowDto { Id = x.Id, D = x.When.DayOfWeek })
            .ToList()
            .Select(c => c.D)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DayOfWeekNestedDtoMember()
    {
        using TestDatabase db = Setup();
        List<HsjMiRow> local = Rows();

        List<int> expected = local
            .Select(x => new HsjMiOuter { Id = x.Id, Inner = new HsjMiInner { D = x.When.DayOfWeek } })
            .Where(o => o.Inner!.D == DayOfWeek.Monday)
            .Select(o => o.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<HsjMiRow>()
            .Select(x => new HsjMiOuter { Id = x.Id, Inner = new HsjMiInner { D = x.When.DayOfWeek } })
            .Where(o => o.Inner!.D == DayOfWeek.Monday)
            .Select(o => o.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DayOfWeekMemberThroughCte()
    {
        using TestDatabase db = Setup();
        List<HsjMiRow> local = Rows();

        SQLiteCte<HsjMiDowDto> cte = db.With<HsjMiDowDto>(() =>
            db.Table<HsjMiRow>().Select(x => new HsjMiDowDto { Id = x.Id, D = x.When.DayOfWeek }));

        List<int> expected = local
            .Select(x => new HsjMiDowDto { Id = x.Id, D = x.When.DayOfWeek })
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
}
