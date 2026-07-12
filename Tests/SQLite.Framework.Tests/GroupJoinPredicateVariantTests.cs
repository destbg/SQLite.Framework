using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GjpvParent")]
public class GjpvParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("GjpvChild")]
public class GjpvChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string Name { get; set; } = "";
}

public class GjpvHolder
{
    public IEnumerable<int> Seq { get; set; } = [];

    public IEnumerable<GjpvChild> Kids { get; set; } = [];
}

public class GroupJoinPredicateVariantTests
{
    private static TestDatabase Setup(out List<GjpvParent> parents, out List<GjpvChild> children)
    {
        TestDatabase db = new();
        db.Table<GjpvParent>().Schema.CreateTable();
        db.Table<GjpvChild>().Schema.CreateTable();
        parents =
        [
            new GjpvParent { Id = 1, Name = "p1" },
            new GjpvParent { Id = 2, Name = "p2" },
        ];
        children =
        [
            new GjpvChild { Id = 1, ParentId = 1, Name = "c1" },
        ];
        db.Table<GjpvParent>().AddRange(parents);
        db.Table<GjpvChild>().AddRange(children);
        return db;
    }

    [Fact]
    public void WhereAfterFlattenedGroupJoinNotUsingGroupMatchesLinq()
    {
        using TestDatabase db = Setup(out List<GjpvParent> parents, out List<GjpvChild> children);

        List<int> expected = (from p in parents
            join c in children on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            where p.Name != "p2"
            select p.Id).ToList();

        List<int> actual = (from p in db.Table<GjpvParent>()
            join c in db.Table<GjpvChild>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            where p.Name != "p2"
            select p.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereWithCapturedSequenceMemberAfterGroupJoinMatchesLinq()
    {
        using TestDatabase db = Setup(out List<GjpvParent> parents, out List<GjpvChild> children);
        GjpvHolder holder = new() { Seq = [1, 3], Kids = [new GjpvChild { Id = 9, ParentId = 9 }] };

        List<int> expected = (from p in parents
            join c in children on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            where holder.Seq.Contains(p.Id) && holder.Kids.Any()
            select p.Id).ToList();

        List<int> actual = (from p in db.Table<GjpvParent>()
            join c in db.Table<GjpvChild>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            where holder.Seq.Contains(p.Id) && holder.Kids.Any()
            select p.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilteredGroupWithClientPredicateThrows()
    {
        using TestDatabase db = Setup(out _, out _);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => (from p in db.Table<GjpvParent>()
            join c in db.Table<GjpvChild>() on p.Id equals c.ParentId into g
            from c in g.Where(k => CmcClientFns.Tag(k.Name) == "[c1]").DefaultIfEmpty()
            select p.Id).ToList());

        Assert.Equal("Unsupported WHERE expression (Tag(k.Name) == \"[c1]\")", ex.Message);
    }
}
