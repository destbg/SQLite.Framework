using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CteJoinSideRows")]
public class CteJoinSideRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class CteJoinSideStep
{
    public int Id { get; set; }

    public int Depth { get; set; }
}

public class CteJoinSideArrayStep
{
    public int Id { get; set; }

    public int[] Tags { get; set; } = [];
}

public class RecursiveCteAsJoinSideTests
{
    [Fact]
    public void ARecursiveCteUsedAsAJoinSideMatchesLinq()
    {
        using TestDatabase db = Setup();

        SQLiteCte<CteJoinSideStep> cte = db.WithRecursive<CteJoinSideStep>(self =>
            db.Table<CteJoinSideRow>()
                .Select(r => new CteJoinSideStep { Id = r.Id, Depth = 0 })
                .Concat(from s in self
                        where s.Depth < 1
                        select new CteJoinSideStep { Id = s.Id, Depth = s.Depth + 1 }));

        List<string> actual = db.Table<CteJoinSideRow>()
            .Join(cte, r => r.Id, s => s.Id, (r, s) => r.Name + s.Depth)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(["a0", "a1", "b0", "b1"], actual);
    }

    [Fact]
    public void ARecursiveCteWithAClientMemberUsedAsAJoinSideMatchesLinq()
    {
        using TestDatabase db = Setup();

        SQLiteCte<CteJoinSideArrayStep> cte = db.WithRecursive<CteJoinSideArrayStep>(self =>
            db.Table<CteJoinSideRow>()
                .Where(r => r.Id == 1)
                .Select(r => new CteJoinSideArrayStep { Id = r.Id, Tags = new[] { r.Id } })
                .Concat(from s in self
                        where s.Id < 2
                        select new CteJoinSideArrayStep { Id = s.Id + 1, Tags = new[] { s.Id } }));

        List<string> actual = db.Table<CteJoinSideRow>()
            .Join(cte, r => r.Id, s => s.Id, (r, s) => r.Name)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(["a", "b"], actual);
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<CteJoinSideRow>().Schema.CreateTable();
        db.Table<CteJoinSideRow>().AddRange(
        [
            new CteJoinSideRow { Id = 1, Name = "a" },
            new CteJoinSideRow { Id = 2, Name = "b" }
        ]);
        return db;
    }
}
