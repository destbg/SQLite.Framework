using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ScalarProjectionGroupRow
{
    [Key]
    public int Id { get; set; }

    public string Grp { get; set; } = "";

    public string Name { get; set; } = "";
}

internal sealed record ScalarKeyRecord(string Key);

public class ScalarParameterConstructorProjectionTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<ScalarProjectionGroupRow>().Schema.CreateTable();
        db.Table<ScalarProjectionGroupRow>().Add(new ScalarProjectionGroupRow { Id = 1, Grp = "x", Name = "a" });
        db.Table<ScalarProjectionGroupRow>().Add(new ScalarProjectionGroupRow { Id = 2, Grp = "x", Name = "b" });
        db.Table<ScalarProjectionGroupRow>().Add(new ScalarProjectionGroupRow { Id = 3, Grp = "y", Name = "c" });
        return db;
    }

    [Fact]
    public void AnonymousTypeOverScalarParameterKeepsTheValue()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<ScalarProjectionGroupRow>().AsEnumerable()
            .Select(r => r.Grp)
            .Select(grp => new { Key = grp })
            .Select(x => x.Key)
            .ToList();

        Assert.Equal(["x", "x", "y"], expected);

        List<string> actual = db.Table<ScalarProjectionGroupRow>()
            .Select(r => r.Grp)
            .Select(grp => new { Key = grp })
            .AsEnumerable()
            .Select(x => x.Key)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnonymousTypeOverScalarIntParameterKeepsTheValue()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<ScalarProjectionGroupRow>().AsEnumerable()
            .Select(r => r.Id)
            .Select(v => new { V = v })
            .Select(x => x.V)
            .ToList();

        Assert.Equal([1, 2, 3], expected);

        List<int> actual = db.Table<ScalarProjectionGroupRow>()
            .Select(r => r.Id)
            .Select(v => new { V = v })
            .AsEnumerable()
            .Select(x => x.V)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PositionalRecordOverScalarParameterKeepsTheValue()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<ScalarProjectionGroupRow>().AsEnumerable()
            .Select(r => r.Grp)
            .Select(grp => new ScalarKeyRecord(grp))
            .Select(x => x.Key)
            .ToList();

        Assert.Equal(["x", "x", "y"], expected);

        List<string> actual = db.Table<ScalarProjectionGroupRow>()
            .Select(r => r.Grp)
            .Select(grp => new ScalarKeyRecord(grp))
            .AsEnumerable()
            .Select(x => x.Key)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinOnScalarRangeVariableProjectsTheKey()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = (from grp in db.Table<ScalarProjectionGroupRow>().AsEnumerable().Select(r => r.Grp).Distinct()
                                 join v in db.Table<ScalarProjectionGroupRow>().AsEnumerable() on grp equals v.Grp
                                 select grp + ":" + v.Name).ToList();

        Assert.Equal(["x:a", "x:b", "y:c"], expected);

        List<string> actual = (from grp in db.Table<ScalarProjectionGroupRow>().Select(r => r.Grp).Distinct()
                               join v in db.Table<ScalarProjectionGroupRow>() on grp equals v.Grp
                               select new { grp, v.Name })
            .AsEnumerable()
            .Select(x => x.grp + ":" + x.Name)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
