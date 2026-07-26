using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22nFrozenNameRows")]
public class H22nFrozenNameRow
{
    public H22nFrozenNameRow(int id, string name)
    {
        Id = id;
        Name = name;
    }

    [Key]
    public int Id { get; set; }

    public string Name { get; }
}

public class ConstructorOnlyMemberRowElementTests
{
    [Fact]
    public void ConstructorOnlyMemberOfARowElementKeepsItsValue()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .Select(a => a[0].Name)
            .ToList();

        List<string> actual = db.Table<H22nFrozenNameRow>()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .ToList()
            .Select(a => a[0].Name)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstructorOnlyMemberRowElementKeepsEveryColumn()
    {
        using TestDatabase db = Setup();

        List<(int Id, string Name)> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .Select(a => (a[0].Id, a[0].Name))
            .ToList();

        List<(int Id, string Name)> actual = db.Table<H22nFrozenNameRow>()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .ToList()
            .Select(a => (a[0].Id, a[0].Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22nFrozenNameRow> Rows()
    {
        return
        [
            new H22nFrozenNameRow(1, "Ann"),
            new H22nFrozenNameRow(2, "Bob")
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22nFrozenNameRow>().Schema.CreateTable();
        db.Table<H22nFrozenNameRow>().AddRange(Rows());
        return db;
    }
}
