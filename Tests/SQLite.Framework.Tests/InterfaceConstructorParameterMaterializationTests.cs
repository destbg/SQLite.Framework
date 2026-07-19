using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20MatCtorRow")]
public class H20MatCtorRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H20MatJoinRow")]
public class H20MatJoinRow
{
    [Key]
    public int Id { get; set; }

    public int RefId { get; set; }

    public string Label { get; set; } = "";
}

public record H20MatIfaceParamRec(IComparable Value);

public sealed class H20PosIfaceNested : IComparable
{
    public int X { get; set; }

    public int CompareTo(object? obj)
    {
        return 0;
    }
}

public class InterfaceConstructorParameterMaterializationTests
{
    private static (List<H20MatCtorRow> Rows, List<H20MatJoinRow> Joins) Seed(TestDatabase db)
    {
        db.Table<H20MatCtorRow>().Schema.CreateTable();
        db.Table<H20MatJoinRow>().Schema.CreateTable();

        List<H20MatCtorRow> rows =
        [
            new H20MatCtorRow { Id = 1, Name = "Ann" },
            new H20MatCtorRow { Id = 2, Name = "Bob" },
        ];
        List<H20MatJoinRow> joins =
        [
            new H20MatJoinRow { Id = 1, RefId = 1, Label = "alpha" },
            new H20MatJoinRow { Id = 2, RefId = 2, Label = "beta" },
        ];

        db.Table<H20MatCtorRow>().AddRange(rows);
        db.Table<H20MatJoinRow>().AddRange(joins);
        return (rows, joins);
    }

    [Fact]
    public void InterfaceTypedConstructorParameterReadsTheColumnValue()
    {
        using TestDatabase db = new();
        db.Table<H20MatCtorRow>().Schema.CreateTable();
        List<H20MatCtorRow> rows =
        [
            new H20MatCtorRow { Id = 1, Name = "Ann" },
            new H20MatCtorRow { Id = 2, Name = "Bob" },
        ];
        db.Table<H20MatCtorRow>().AddRange(rows);

        List<string> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => new H20MatIfaceParamRec(r.Name).Value.ToString()!)
            .ToList();

        List<string> actual = db.Table<H20MatCtorRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H20MatIfaceParamRec(r.Name))
            .ToList()
            .Select(x => x.Value.ToString()!)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinProjectionInterfaceParameterReadsTheColumnValue()
    {
        using TestDatabase db = new();
        var (rows, joins) = Seed(db);

        List<string> expected = (from r in rows
                join j in joins on r.Id equals j.RefId
                orderby r.Id
                select new H20MatIfaceParamRec(j.Label).Value.ToString()!)
            .ToList();

        List<string> actual = (from r in db.Table<H20MatCtorRow>()
                join j in db.Table<H20MatJoinRow>() on r.Id equals j.RefId
                orderby r.Id
                select new H20MatIfaceParamRec(j.Label))
            .ToList()
            .Select(x => x.Value.ToString()!)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectBeforeJoinInterfaceParameterReadsTheColumnValue()
    {
        using TestDatabase db = new();
        var (rows, joins) = Seed(db);

        List<string> expected = rows
            .Select(r => new { Foo = r.Id })
            .Join(joins, a => a.Foo, j => j.RefId, (a, j) => new H20MatIfaceParamRec(j.Label).Value.ToString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.Table<H20MatCtorRow>()
            .Select(r => new { Foo = r.Id })
            .Join(db.Table<H20MatJoinRow>(), a => a.Foo, j => j.RefId, (a, j) => new H20MatIfaceParamRec(j.Label))
            .ToList()
            .Select(x => x.Value.ToString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectedValueKeyInterfaceParameterReadsWithTheProjectedType()
    {
        using TestDatabase db = new();
        var (rows, joins) = Seed(db);

        List<string> expected = rows
            .Select(r => new { Value = r.Id })
            .Join(joins, a => a.Value, j => j.RefId, (a, j) => new H20MatIfaceParamRec(j.Label).Value.ToString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.Table<H20MatCtorRow>()
            .Select(r => new { Value = r.Id })
            .Join(db.Table<H20MatJoinRow>(), a => a.Value, j => j.RefId, (a, j) => new H20MatIfaceParamRec(j.Label))
            .ToList()
            .Select(x => x.Value.ToString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedCompositeInterfaceParameterReadsAsObject()
    {
        using TestDatabase db = new();
        db.Table<H20MatCtorRow>().Schema.CreateTable();
        db.Table<H20MatCtorRow>().Add(new H20MatCtorRow { Id = 1, Name = "Ann" });

        List<int> rows = db.Table<H20MatCtorRow>()
            .Select(r => new H20MatIfaceParamRec(new H20PosIfaceNested { X = r.Id }))
            .ToList()
            .Select(r => ((H20PosIfaceNested)r.Value).X)
            .ToList();

        Assert.Equal([1], rows);
    }
}
