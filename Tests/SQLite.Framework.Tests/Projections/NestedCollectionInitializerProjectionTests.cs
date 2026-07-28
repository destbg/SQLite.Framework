using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23bNestedListRows")]
public class H23bNestedListRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H23bNestedListInner
{
    public string Label { get; set; } = "";

    public List<string> Tags { get; } = new();
}

public class H23bNestedListOuter
{
    public int Id { get; set; }

    public H23bNestedListInner Inner { get; set; } = new();
}

public class NestedCollectionInitializerProjectionTests
{
    [Fact]
    public void CollectionInitializerOnANestedProjectedObjectKeepsItsConstantElements()
    {
        using TestDatabase db = Setup(nameof(CollectionInitializerOnANestedProjectedObjectKeepsItsConstantElements));

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H23bNestedListOuter
            {
                Id = r.Id,
                Inner = new H23bNestedListInner { Label = r.Name, Tags = { "fixed" } }
            })
            .ToList()
            .Select(o => string.Join(",", o.Inner.Tags) + ":" + o.Inner.Label)
            .ToList();

        List<string> actual = db.Table<H23bNestedListRow>().OrderBy(r => r.Id)
            .Select(r => new H23bNestedListOuter
            {
                Id = r.Id,
                Inner = new H23bNestedListInner { Label = r.Name, Tags = { "fixed" } }
            })
            .ToList()
            .Select(o => string.Join(",", o.Inner.Tags) + ":" + o.Inner.Label)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CollectionInitializerOnANestedProjectedObjectKeepsItsColumnElements()
    {
        using TestDatabase db = Setup(nameof(CollectionInitializerOnANestedProjectedObjectKeepsItsColumnElements));

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H23bNestedListOuter
            {
                Id = r.Id,
                Inner = new H23bNestedListInner { Label = r.Name, Tags = { r.Name } }
            })
            .ToList()
            .Select(o => string.Join(",", o.Inner.Tags) + ":" + o.Inner.Label)
            .ToList();

        List<string> actual = db.Table<H23bNestedListRow>().OrderBy(r => r.Id)
            .Select(r => new H23bNestedListOuter
            {
                Id = r.Id,
                Inner = new H23bNestedListInner { Label = r.Name, Tags = { r.Name } }
            })
            .ToList()
            .Select(o => string.Join(",", o.Inner.Tags) + ":" + o.Inner.Label)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23bNestedListRow> Rows()
    {
        return
        [
            new H23bNestedListRow { Id = 1, Name = "alpha" },
            new H23bNestedListRow { Id = 2, Name = "beta" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23bNestedListRow>().Schema.CreateTable();
        db.Table<H23bNestedListRow>().AddRange(Rows());
        return db;
    }
}
