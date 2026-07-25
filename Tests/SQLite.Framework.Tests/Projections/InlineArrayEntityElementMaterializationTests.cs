using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21eArrRecs")]
public record H21eArrRec([property: Key] int Id, string Name);

[Table("H21eArrFrozen")]
public class H21eArrFrozenRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Tag { get; } = "steady";
}

[Table("H21eArrPlain")]
public class H21eArrPlainRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class InlineArrayEntityElementMaterializationTests
{
    private static List<H21eArrRec> Records()
    {
        return
        [
            new H21eArrRec(1, "Ann"),
            new H21eArrRec(2, "Bob")
        ];
    }

    private static List<H21eArrFrozenRow> FrozenRows()
    {
        return
        [
            new H21eArrFrozenRow { Id = 1, Name = "Ann" },
            new H21eArrFrozenRow { Id = 2, Name = "Bob" }
        ];
    }

    private static List<H21eArrPlainRow> PlainRows()
    {
        return
        [
            new H21eArrPlainRow { Id = 1, Name = "Ann" },
            new H21eArrPlainRow { Id = 2, Name = "Bob" }
        ];
    }

    [Fact]
    public void PositionalEntityElementKeepsTheWholeRow()
    {
        using TestDatabase db = new();
        db.Table<H21eArrRec>().Schema.CreateTable();
        db.Table<H21eArrRec>().AddRange(Records());
        List<H21eArrRec> local = Records();

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .Select(a => a[0].Name)
            .ToList();

        List<string> actual = db.Table<H21eArrRec>()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .ToList()
            .Select(a => a[0].Name)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetOnlyColumnEntityElementKeepsTheWholeRow()
    {
        using TestDatabase db = new();
        db.Table<H21eArrFrozenRow>().Schema.CreateTable();
        db.Table<H21eArrFrozenRow>().AddRange(FrozenRows());
        List<H21eArrFrozenRow> local = FrozenRows();

        List<(string Name, string Tag)> expected = local
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .Select(a => (a[0].Name, a[0].Tag))
            .ToList();

        List<(string Name, string Tag)> actual = db.Table<H21eArrFrozenRow>()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .ToList()
            .Select(a => (a[0].Name, a[0].Tag))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SettableEntityElementKeepsTheWholeRow()
    {
        using TestDatabase db = new();
        db.Table<H21eArrPlainRow>().Schema.CreateTable();
        db.Table<H21eArrPlainRow>().AddRange(PlainRows());
        List<H21eArrPlainRow> local = PlainRows();

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .Select(a => a[0].Name)
            .ToList();

        List<string> actual = db.Table<H21eArrPlainRow>()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .ToList()
            .Select(a => a[0].Name)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
