using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SubqNullCmpItems")]
public class SubqNullCmpItem
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("SubqNullCmpChildren")]
public class SubqNullCmpChild
{
    [Key]
    public int Id { get; set; }

    public int ItemId { get; set; }

    public string Note { get; set; } = "";
}

public class SubqueryEntityNullComparisonTests
{
    private static List<SubqNullCmpItem> Items()
    {
        return
        [
            new SubqNullCmpItem { Id = 1, Name = "with child" },
            new SubqNullCmpItem { Id = 2, Name = "without child" },
            new SubqNullCmpItem { Id = 3, Name = "with two children" },
        ];
    }

    private static List<SubqNullCmpChild> Children()
    {
        return
        [
            new SubqNullCmpChild { Id = 1, ItemId = 1, Note = "a" },
            new SubqNullCmpChild { Id = 2, ItemId = 3, Note = "b" },
            new SubqNullCmpChild { Id = 3, ItemId = 3, Note = "c" },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<SubqNullCmpItem>().Schema.CreateTable();
        db.Table<SubqNullCmpItem>().AddRange(Items());
        db.Table<SubqNullCmpChild>().Schema.CreateTable();
        db.Table<SubqNullCmpChild>().AddRange(Children());
        return db;
    }

    [Fact]
    public void FirstOrDefaultComparedToNullKeepsRowsWithoutMatch()
    {
        using TestDatabase db = Seed();
        List<SubqNullCmpItem> items = Items();
        List<SubqNullCmpChild> children = Children();

        List<int> expected = items
            .Where(i => children.FirstOrDefault(c => c.ItemId == i.Id) == null)
            .Select(i => i.Id)
            .OrderBy(x => x)
            .ToList();
        List<int> actual = db.Table<SubqNullCmpItem>()
            .Where(i => db.Table<SubqNullCmpChild>().FirstOrDefault(c => c.ItemId == i.Id) == null)
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefaultComparedToNotNullKeepsRowsWithMatch()
    {
        using TestDatabase db = Seed();
        List<SubqNullCmpItem> items = Items();
        List<SubqNullCmpChild> children = Children();

        List<int> expected = items
            .Where(i => children.FirstOrDefault(c => c.ItemId == i.Id) != null)
            .Select(i => i.Id)
            .OrderBy(x => x)
            .ToList();
        List<int> actual = db.Table<SubqNullCmpItem>()
            .Where(i => db.Table<SubqNullCmpChild>().FirstOrDefault(c => c.ItemId == i.Id) != null)
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
