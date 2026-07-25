using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GcParents")]
file sealed class GcParent
{
    [Key]
    public int Id { get; set; }
}

[Table("GcChildren")]
file sealed class GcChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string? Title { get; set; }
}

public class GroupConcatNullSemanticsTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<GcParent>().Schema.CreateTable();
        db.Table<GcChild>().Schema.CreateTable();
        db.Table<GcParent>().Add(new GcParent { Id = 1 });
        db.Table<GcParent>().Add(new GcParent { Id = 2 });
        db.Table<GcChild>().Add(new GcChild { Id = 1, ParentId = 1, Title = "x" });
        db.Table<GcChild>().Add(new GcChild { Id = 2, ParentId = 1, Title = null });
        db.Table<GcChild>().Add(new GcChild { Id = 3, ParentId = 1, Title = "z" });
        return db;
    }

    [Fact]
    public void StringJoinKeepsNullElementsAsEmptyWithSeparators()
    {
        using TestDatabase db = Seed();

        string?[] titles = ["x", null, "z"];
        string oracle = string.Join(",", titles);
        Assert.Equal("x,,z", oracle);

        string actual = db.Table<GcParent>()
            .Where(p => p.Id == 1)
            .Select(p => string.Join(",", db.Table<GcChild>()
                .Where(c => c.ParentId == p.Id)
                .OrderBy(c => c.Id)
                .Select(c => c.Title)))
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void StringJoinOverEmptySequenceReturnsEmptyString()
    {
        using TestDatabase db = Seed();

        string oracle = string.Join(",", System.Array.Empty<string>());
        Assert.Equal("", oracle);

        string actual = db.Table<GcParent>()
            .Where(p => p.Id == 2)
            .Select(p => string.Join(",", db.Table<GcChild>()
                .Where(c => c.ParentId == p.Id)
                .OrderBy(c => c.Id)
                .Select(c => c.Title)))
            .First();

        Assert.Equal(oracle, actual);
    }
}
