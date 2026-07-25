using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21kDefaultRows")]
public class H21kDefaultRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H21kDefaultView
{
    public int Id { get; set; }

    public string Note { get; set; } = "unset";

    public int Score { get; set; } = 7;
}

public class H21kTagBox
{
    public string Label { get; set; } = "";

    public List<string> Tags { get; set; } = [];
}

public class H21kBoxHolder
{
    public int Id { get; set; }

    public H21kTagBox? Inner { get; set; }
}

public class ProjectionPropertyDefaultValueTests
{
    private static List<H21kDefaultRow> Rows()
    {
        return
        [
            new H21kDefaultRow { Id = 1, Name = "a" },
            new H21kDefaultRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21kDefaultRow>().Schema.CreateTable();
        db.Table<H21kDefaultRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void UnassignedStringPropertyKeepsItsDeclaredDefault()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H21kDefaultView { Id = r.Id })
            .Select(v => v.Note)
            .ToList();

        List<string> actual = db.Table<H21kDefaultRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H21kDefaultView { Id = r.Id })
            .ToList()
            .Select(v => v.Note)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnassignedNumberPropertyKeepsItsDeclaredDefault()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H21kDefaultView { Id = r.Id })
            .Select(v => v.Score)
            .ToList();

        List<int> actual = db.Table<H21kDefaultRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H21kDefaultView { Id = r.Id })
            .ToList()
            .Select(v => v.Score)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnassignedNestedCollectionPropertyKeepsItsDeclaredDefault()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H21kBoxHolder { Id = r.Id, Inner = new H21kTagBox { Label = r.Name } })
            .Select(h => h.Inner is null ? -2 : h.Inner.Tags is null ? -1 : h.Inner.Tags.Count)
            .ToList();

        List<int> actual = db.Table<H21kDefaultRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H21kBoxHolder { Id = r.Id, Inner = new H21kTagBox { Label = r.Name } })
            .ToList()
            .Select(h => h.Inner is null ? -2 : h.Inner.Tags is null ? -1 : h.Inner.Tags.Count)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
