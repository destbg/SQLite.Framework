using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CtorInitSourceRow
{
    [Key] public int Id { get; set; }
    public int N { get; set; }
    public string S { get; set; } = "";
}

public record CtorInitRecord(int Id, string Label)
{
    public int Extra { get; init; }
}

public class CtorInitClass
{
    public CtorInitClass(int id, string label)
    {
        Id = id;
        Label = label;
    }

    public int Id { get; }
    public string Label { get; }
    public int Extra { get; set; }
}

public class ConstructorArgumentInitializerProjectionParityTests
{
    private static TestDatabase Seed(out List<CtorInitSourceRow> rows)
    {
        TestDatabase db = new();
        db.Table<CtorInitSourceRow>().Schema.CreateTable();
        rows = new()
        {
            new() { Id = 1, N = 5, S = "alpha" },
            new() { Id = 2, N = -3, S = "beta" },
        };
        foreach (CtorInitSourceRow r in rows)
        {
            db.Table<CtorInitSourceRow>().Add(r);
        }
        return db;
    }

    [Fact]
    public void RecordConstructorArgumentsWithInitMember_AreNotLost()
    {
        using TestDatabase db = Seed(out List<CtorInitSourceRow> rows);

        List<CtorInitRecord> expected = rows.OrderBy(r => r.Id)
            .Select(r => new CtorInitRecord(r.Id, r.S) { Extra = r.N }).ToList();
        List<CtorInitRecord> actual = db.Table<CtorInitSourceRow>().OrderBy(r => r.Id)
            .Select(r => new CtorInitRecord(r.Id, r.S) { Extra = r.N }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClassConstructorArgumentsWithInitMember_AreNotLost()
    {
        using TestDatabase db = Seed(out List<CtorInitSourceRow> rows);

        List<(int, string, int)> expected = rows.OrderBy(r => r.Id)
            .Select(r => new CtorInitClass(r.Id, r.S) { Extra = r.N })
            .Select(x => (x.Id, x.Label, x.Extra)).ToList();
        List<(int, string, int)> actual = db.Table<CtorInitSourceRow>().OrderBy(r => r.Id)
            .Select(r => new CtorInitClass(r.Id, r.S) { Extra = r.N }).ToList()
            .Select(x => (x.Id, x.Label, x.Extra)).ToList();

        Assert.Equal(expected, actual);
    }
}
