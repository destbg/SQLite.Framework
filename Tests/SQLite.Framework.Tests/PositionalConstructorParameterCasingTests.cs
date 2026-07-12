using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("HsjCaseRows")]
public class HsjCaseRow
{
    [Key]
    public int Id { get; set; }

    public int Alpha { get; set; }

    public int Beta { get; set; }
}

public class HsjCasePair
{
    public HsjCasePair(int id, int total)
    {
        Id = id;
        Total = total;
    }

    public int Id { get; }

    public int Total { get; }
}

public class PositionalConstructorParameterCasingTests
{
    private static List<HsjCaseRow> Rows()
    {
        return
        [
            new HsjCaseRow { Id = 1, Alpha = 10, Beta = 20 },
            new HsjCaseRow { Id = 2, Alpha = 100, Beta = 30 },
            new HsjCaseRow { Id = 3, Alpha = 40, Beta = 50 },
            new HsjCaseRow { Id = 4, Alpha = 5, Beta = 8 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<HsjCaseRow>().Schema.CreateTable();
        db.Table<HsjCaseRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ChainedWhereOnProjectedMember()
    {
        using TestDatabase db = Setup();
        List<HsjCaseRow> local = Rows();

        List<int> expected = local
            .Select(x => new HsjCasePair(x.Id, x.Alpha + x.Beta))
            .Where(v => v.Total > 50)
            .Select(v => v.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<HsjCaseRow>()
            .Select(x => new HsjCasePair(x.Id, x.Alpha + x.Beta))
            .Where(v => v.Total > 50)
            .Select(v => v.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ChainedOrderByOnProjectedMember()
    {
        using TestDatabase db = Setup();
        List<HsjCaseRow> local = Rows();

        List<int> expected = local
            .Select(x => new HsjCasePair(x.Id, x.Alpha + x.Beta))
            .OrderBy(v => v.Total)
            .Select(v => v.Id)
            .ToList();

        List<int> actual = db.Table<HsjCaseRow>()
            .Select(x => new HsjCasePair(x.Id, x.Alpha + x.Beta))
            .OrderBy(v => v.Total)
            .Select(v => v.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
