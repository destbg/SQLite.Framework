using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("prj_nconcp_row")]
public class NestedConstructedRow
{
    [Key]
    public int Id { get; set; }

    public string? Note { get; set; }

    public int? Amount { get; set; }
}

public class NestedConstructedChild
{
    public string? Note { get; set; }

    public int? Amount { get; set; }
}

public class NestedConstructedChildWithDefault
{
    public string? Note { get; set; } = "def";

    public int? Amount { get; set; } = 7;
}

public class NestedConstructedParentNoInit
{
    public int Id { get; set; }

    public NestedConstructedChild Child { get; set; } = null!;
}

public class NestedConstructedParentDefaultChild
{
    public int Id { get; set; }

    public NestedConstructedChildWithDefault Child { get; set; } = new();
}

[Table("prj_nconcp_child")]
public class NestedConstructedJoinChild
{
    [Key]
    public int Id { get; set; }

    public int RowId { get; set; }

    public string? Title { get; set; }
}

public class NestedConstructedMixedWrap
{
    public string? Tag { get; set; }

    public NestedConstructedJoinChild? Entity { get; set; }

    public NestedConstructedChild? Extra { get; set; }
}

public class NestedConstructedObjectNullColumnProjectionTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<NestedConstructedRow>().Schema.CreateTable();
        db.Table<NestedConstructedRow>().Add(new NestedConstructedRow { Id = 1, Note = null, Amount = null });
        db.Table<NestedConstructedRow>().Add(new NestedConstructedRow { Id = 2, Note = "x", Amount = 5 });
        return db;
    }

    [Fact]
    public void ExplicitChildWithoutDefaultInitializerStaysNonNull()
    {
        using TestDatabase db = Seed();

        List<bool> expected = db.Table<NestedConstructedRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => new NestedConstructedParentNoInit { Id = r.Id, Child = new NestedConstructedChild { Note = r.Note, Amount = r.Amount } })
            .Select(p => p.Child != null)
            .ToList();

        Assert.Equal(new List<bool> { true, true }, expected);

        List<bool> actual = db.Table<NestedConstructedRow>()
            .OrderBy(r => r.Id)
            .Select(r => new NestedConstructedParentNoInit { Id = r.Id, Child = new NestedConstructedChild { Note = r.Note, Amount = r.Amount } })
            .AsEnumerable()
            .Select(p => p.Child != null)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExplicitChildOverwritesDefaultMembersWithNull()
    {
        using TestDatabase db = Seed();

        List<string> expected = db.Table<NestedConstructedRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => new NestedConstructedParentDefaultChild { Id = r.Id, Child = new NestedConstructedChildWithDefault { Note = r.Note, Amount = r.Amount } })
            .Select(p => p.Id + ":" + (p.Child.Note ?? "<n>") + ":" + (p.Child.Amount?.ToString() ?? "<n>"))
            .ToList();

        Assert.Equal(new List<string> { "1:<n>:<n>", "2:x:5" }, expected);

        List<string> actual = db.Table<NestedConstructedRow>()
            .OrderBy(r => r.Id)
            .Select(r => new NestedConstructedParentDefaultChild { Id = r.Id, Child = new NestedConstructedChildWithDefault { Note = r.Note, Amount = r.Amount } })
            .AsEnumerable()
            .Select(p => p.Id + ":" + (p.Child.Note ?? "<n>") + ":" + (p.Child.Amount?.ToString() ?? "<n>"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MixedEntityAndConstructedMembersFollowTheirOwnNullRules()
    {
        using TestDatabase db = Seed();
        db.Table<NestedConstructedJoinChild>().Schema.CreateTable();
        db.Table<NestedConstructedJoinChild>().Add(new NestedConstructedJoinChild { Id = 10, RowId = 2, Title = "t2" });

        List<NestedConstructedRow> rows = db.Table<NestedConstructedRow>().ToList();
        List<NestedConstructedJoinChild> children = db.Table<NestedConstructedJoinChild>().ToList();

        List<string> expected = (from r in rows
                                 join c in children on r.Id equals c.RowId into grp
                                 from c in grp.DefaultIfEmpty()
                                 select new NestedConstructedMixedWrap { Tag = r.Note, Entity = c, Extra = new NestedConstructedChild { Note = r.Note, Amount = r.Amount } })
            .OrderBy(w => w.Extra!.Amount ?? 0)
            .Select(w => (w.Tag ?? "<n>") + "|" + (w.Entity == null ? "<n>" : w.Entity.Title) + "|" + (w.Extra == null ? "<n>" : w.Extra.Note ?? "<e>"))
            .ToList();

        List<string> actual = (from r in db.Table<NestedConstructedRow>()
                               join c in db.Table<NestedConstructedJoinChild>() on r.Id equals c.RowId into grp
                               from c in grp.DefaultIfEmpty()
                               select new NestedConstructedMixedWrap { Tag = r.Note, Entity = c, Extra = new NestedConstructedChild { Note = r.Note, Amount = r.Amount } })
            .AsEnumerable()
            .OrderBy(w => w.Extra!.Amount ?? 0)
            .Select(w => (w.Tag ?? "<n>") + "|" + (w.Entity == null ? "<n>" : w.Entity.Title) + "|" + (w.Extra == null ? "<n>" : w.Extra.Note ?? "<e>"))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
