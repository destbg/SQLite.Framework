using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22nSideRows")]
public class H22nSideRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }

    public string Name { get; set; } = "";
}

public class H22nSidePart
{
    public H22nSidePart(int amount, string label)
    {
        Amount = amount;
        Label = label;
    }

    public int Amount { get; }

    public string Label { get; set; } = "";
}

public class H22nSideHolder
{
    public int Id { get; set; }

    public H22nSidePart? Side { get; set; }
}

public class NestedConstructorOnlyMemberProjectionTests
{
    [Fact]
    public void ConstructorOnlyMemberOfANestedAnonymousPartKeepsItsValue()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Side = new H22nSidePart(r.Value, r.Name) })
            .Select(x => x.Side.Amount)
            .ToList();

        List<int> actual = db.Table<H22nSideRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Side = new H22nSidePart(r.Value, r.Name) })
            .ToList()
            .Select(x => x.Side.Amount)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstructorOnlyMemberOfANestedNamedPartKeepsItsValue()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H22nSideHolder { Id = r.Id, Side = new H22nSidePart(r.Value, r.Name) })
            .Select(h => h.Side!.Amount)
            .ToList();

        List<int> actual = db.Table<H22nSideRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H22nSideHolder { Id = r.Id, Side = new H22nSidePart(r.Value, r.Name) })
            .ToList()
            .Select(h => h.Side!.Amount)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22nSideRow> Rows()
    {
        return
        [
            new H22nSideRow { Id = 1, Value = 10, Name = "a" },
            new H22nSideRow { Id = 2, Value = 20, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22nSideRow>().Schema.CreateTable();
        db.Table<H22nSideRow>().AddRange(Rows());
        return db;
    }
}
