using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
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
