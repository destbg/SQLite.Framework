using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20MatNestRow")]
public class H20MatNestRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H20MatNestInnerObject
{
    public object? Val { get; set; }
}

public class H20MatNestOuterObject
{
    public H20MatNestInnerObject? Inner { get; set; }
}

public class H20MatNestInnerIface
{
    public IComparable? Val { get; set; }
}

public class H20MatNestOuterIface
{
    public H20MatNestInnerIface? Inner { get; set; }
}

public class NestedInitObjectMemberMaterializationTests
{
    private static List<H20MatNestRow> Rows()
    {
        return
        [
            new H20MatNestRow { Id = 1, Name = "Ann" },
            new H20MatNestRow { Id = 2, Name = "Bob" },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<H20MatNestRow>().Schema.CreateTable();
        db.Table<H20MatNestRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ObjectMemberInsideANestedMemberInitMaterializesTheNestedObject()
    {
        using TestDatabase db = Seed();

        List<object?> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H20MatNestOuterObject { Inner = new H20MatNestInnerObject { Val = r.Id } })
            .Select(x => x.Inner?.Val)
            .ToList();

        List<object?> actual = db.Table<H20MatNestRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H20MatNestOuterObject { Inner = new H20MatNestInnerObject { Val = r.Id } })
            .ToList()
            .Select(x => x.Inner?.Val)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InterfaceMemberInsideANestedMemberInitMaterializesTheNestedObject()
    {
        using TestDatabase db = Seed();

        List<string?> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H20MatNestOuterIface { Inner = new H20MatNestInnerIface { Val = r.Name } })
            .Select(x => x.Inner?.Val?.ToString())
            .ToList();

        List<string?> actual = db.Table<H20MatNestRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H20MatNestOuterIface { Inner = new H20MatNestInnerIface { Val = r.Name } })
            .ToList()
            .Select(x => x.Inner?.Val?.ToString())
            .ToList();

        Assert.Equal(expected, actual);
    }
}
