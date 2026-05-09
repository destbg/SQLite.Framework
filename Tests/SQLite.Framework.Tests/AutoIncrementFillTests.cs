using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AutoIncrementFillTests
{
    [Fact]
    public void Add_AutoIncrementInt_FillsId()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();

        Article a = new() { Title = "t", Body = "b", PublishedAt = DateTime.UtcNow };
        Assert.Equal(0, a.Id);

        db.Table<Article>().Add(a);

        Assert.True(a.Id > 0);
    }

    [Fact]
    public void Add_AutoIncrementInt_TwoInRow_GetSequentialIds()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();

        Article a1 = new() { Title = "a", Body = "b", PublishedAt = DateTime.UtcNow };
        Article a2 = new() { Title = "c", Body = "d", PublishedAt = DateTime.UtcNow };

        db.Table<Article>().Add(a1);
        db.Table<Article>().Add(a2);

        Assert.True(a1.Id > 0);
        Assert.True(a2.Id > 0);
        Assert.NotEqual(a1.Id, a2.Id);
    }

    [Fact]
    public void Add_NoAutoIncrement_LeavesIdUntouched()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        Book b = new() { Id = 7, Title = "x", AuthorId = 1, Price = 1 };
        db.Table<Book>().Add(b);

        Assert.Equal(7, b.Id);
    }

    [Fact]
    public void Add_CompositePrimaryKey_NoFill()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<CompositeKeyEntity>();

        CompositeKeyEntity e = new() { ProjectId = 1, TagId = 10, Note = "n" };
        db.Table<CompositeKeyEntity>().Add(e);

        Assert.Equal(1, e.ProjectId);
        Assert.Equal(10, e.TagId);
    }

    [Fact]
    public void Add_AutoIncrementLong_FillsId()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<LongAiEntity>();

        LongAiEntity e = new() { Name = "n" };
        db.Table<LongAiEntity>().Add(e);

        Assert.True(e.Id > 0L);
    }

    [Fact]
    public void Add_AutoIncrementShort_FillsId()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<ShortAiEntity>();

        ShortAiEntity e = new() { Name = "n" };
        db.Table<ShortAiEntity>().Add(e);

        Assert.True(e.Id > (short)0);
    }

    [Fact]
    public void AddRange_AutoIncrement_FillsEveryItem()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();

        Article[] articles =
        {
            new() { Title = "a", Body = "b", PublishedAt = DateTime.UtcNow },
            new() { Title = "c", Body = "d", PublishedAt = DateTime.UtcNow },
            new() { Title = "e", Body = "f", PublishedAt = DateTime.UtcNow }
        };

        db.Table<Article>().AddRange(articles);

        Assert.All(articles, a => Assert.True(a.Id > 0));
        Assert.Equal(3, articles.Select(a => a.Id).Distinct().Count());
    }

    [Fact]
    public void AddOrUpdate_Replace_AutoIncrement_FillsId()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();

        Article a = new() { Title = "t", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().AddOrUpdate(a);

        Assert.True(a.Id > 0);
    }

    [Fact]
    public void AddOrUpdate_Ignore_NoChange_LeavesIdUntouched()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<UniqueAiEntity>();

        UniqueAiEntity first = new() { Code = "X", Name = "first" };
        db.Table<UniqueAiEntity>().Add(first);
        Assert.True(first.Id > 0);

        UniqueAiEntity duplicate = new() { Code = "X", Name = "second" };
        db.Table<UniqueAiEntity>().AddOrUpdate(duplicate, SQLiteConflict.Ignore);

        Assert.Equal(0, duplicate.Id);
    }

    [Fact]
    public void AddRange_WithHooks_StillFillsId()
    {
        using TestDatabase db = new(b =>
            b.OnAction((db_, item, action) => action));
        db.Schema.CreateTable<Article>();

        Article a = new() { Title = "t", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().AddRange(new[] { a });

        Assert.True(a.Id > 0);
    }

    [Fact]
    public void Add_HookSkips_DoesNotFillId()
    {
        using TestDatabase db = new(b =>
            b.OnAction((db_, item, action) => SQLiteAction.Skip));
        db.Schema.CreateTable<Article>();

        Article a = new() { Title = "t", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().Add(a);

        Assert.Equal(0, a.Id);
    }

    [Fact]
    public void Add_AutoIncrement_PreSetId_OverwrittenByGenerated()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();

        Article a = new() { Id = 42, Title = "t", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().Add(a);

        Assert.NotEqual(42, a.Id);
        Assert.True(a.Id > 0);
    }

    [Fact]
    public void AddOrUpdate_AutoIncrement_PreSetIdMatchingExisting_ReplacesRow()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();

        Article original = new() { Title = "old", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().Add(original);

        Article edit = new() { Id = original.Id, Title = "new", Body = "b2", PublishedAt = original.PublishedAt };
        db.Table<Article>().AddOrUpdate(edit);

        List<Article> rows = db.Table<Article>().ToList();
        Article single = Assert.Single(rows);
        Assert.Equal(original.Id, single.Id);
        Assert.Equal("new", single.Title);
        Assert.Equal("b2", single.Body);
        Assert.Equal(original.Id, edit.Id);
    }

    [Fact]
    public void AddOrUpdate_AutoIncrement_PreSetIdNotMatching_InsertsAtThatId()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();

        Article a = new() { Id = 99, Title = "t", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().AddOrUpdate(a);

        Assert.Equal(99, a.Id);
        Article fetched = db.Table<Article>().First(x => x.Id == 99);
        Assert.Equal("t", fetched.Title);
    }

    [Fact]
    public void AddOrUpdate_AutoIncrement_DefaultId_StillAutoAssigns()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();

        Article seed = new() { Title = "seed", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().Add(seed);

        Article fresh = new() { Title = "fresh", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().AddOrUpdate(fresh);

        Assert.True(fresh.Id > 0);
        Assert.NotEqual(seed.Id, fresh.Id);
        Assert.Equal(2, db.Table<Article>().Count());
    }

    [Fact]
    public void Add_AutoIncrement_PreserveOption_PreSetId_UsedDirectly()
    {
        using TestDatabase db = new(b => b.PreserveExplicitAutoIncrementKeys());
        db.Schema.CreateTable<Article>();

        Article a = new() { Id = 42, Title = "t", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().Add(a);

        Assert.Equal(42, a.Id);
        Article fetched = db.Table<Article>().First(x => x.Id == 42);
        Assert.Equal("t", fetched.Title);
    }

    [Fact]
    public void Add_AutoIncrement_PreserveOption_DefaultId_StillAutoAssigns()
    {
        using TestDatabase db = new(b => b.PreserveExplicitAutoIncrementKeys());
        db.Schema.CreateTable<Article>();

        Article a = new() { Title = "t", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().Add(a);

        Assert.True(a.Id > 0);
    }

    [Fact]
    public void Add_AutoIncrement_PreserveOption_PreSetIdMatchingExisting_Throws()
    {
        using TestDatabase db = new(b => b.PreserveExplicitAutoIncrementKeys());
        db.Schema.CreateTable<Article>();

        Article first = new() { Id = 7, Title = "first", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().Add(first);

        Article duplicate = new() { Id = 7, Title = "duplicate", Body = "b", PublishedAt = DateTime.UtcNow };
        Assert.Throws<SQLiteException>(() => db.Table<Article>().Add(duplicate));
    }

    [Fact]
    public void AddRange_AutoIncrement_PreserveOption_MixedIds()
    {
        using TestDatabase db = new(b => b.PreserveExplicitAutoIncrementKeys());
        db.Schema.CreateTable<Article>();

        Article explicitId = new() { Id = 100, Title = "explicit", Body = "b", PublishedAt = DateTime.UtcNow };
        Article auto = new() { Title = "auto", Body = "b", PublishedAt = DateTime.UtcNow };
        Article anotherExplicit = new() { Id = 200, Title = "another", Body = "b", PublishedAt = DateTime.UtcNow };

        db.Table<Article>().AddRange(new[] { explicitId, auto, anotherExplicit });

        Assert.Equal(100, explicitId.Id);
        Assert.Equal(200, anotherExplicit.Id);
        Assert.True(auto.Id > 0 && auto.Id != 100 && auto.Id != 200);
        Assert.Equal(3, db.Table<Article>().Count());
    }

    [Fact]
    public void AddOrUpdateRange_AutoIncrement_MixedIds()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Article>();

        Article seeded = new() { Title = "seed", Body = "b", PublishedAt = DateTime.UtcNow };
        db.Table<Article>().Add(seeded);
        int seededId = seeded.Id;

        Article replace = new() { Id = seededId, Title = "replaced", Body = "b2", PublishedAt = DateTime.UtcNow };
        Article insertAtId = new() { Id = 500, Title = "explicit", Body = "b3", PublishedAt = DateTime.UtcNow };
        Article auto = new() { Title = "auto", Body = "b4", PublishedAt = DateTime.UtcNow };

        db.Table<Article>().AddOrUpdateRange(new[] { replace, insertAtId, auto });

        Assert.Equal(seededId, replace.Id);
        Assert.Equal(500, insertAtId.Id);
        Assert.True(auto.Id > 0 && auto.Id != seededId && auto.Id != 500);

        List<Article> rows = db.Table<Article>().OrderBy(a => a.Id).ToList();
        Assert.Equal(3, rows.Count);
        Assert.Equal("replaced", rows.First(r => r.Id == seededId).Title);
        Assert.Equal("explicit", rows.First(r => r.Id == 500).Title);
        Assert.Equal("auto", rows.First(r => r.Id == auto.Id).Title);
    }
}

[Table("CompositeKeyEntity_AI")]
file class CompositeKeyEntity
{
    [Key] public int ProjectId { get; set; }
    [Key] public int TagId { get; set; }
    public string Note { get; set; } = string.Empty;
}

[Table("LongAi")]
file class LongAiEntity
{
    [Key]
    [AutoIncrement]
    public long Id { get; set; }

    public required string Name { get; set; }
}

[Table("ShortAi")]
file class ShortAiEntity
{
    [Key]
    [AutoIncrement]
    public short Id { get; set; }

    public required string Name { get; set; }
}

[Table("UniqueAi")]
file class UniqueAiEntity
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Indexed(IsUnique = true)]
    public required string Code { get; set; }

    public required string Name { get; set; }
}
