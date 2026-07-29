using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24mBindingOrderRows")]
public class H24mBindingOrderRow
{
    [Key]
    public int Id { get; set; }

    public int First { get; set; }

    public int Second { get; set; }
}

public class H24mBindingOrderDto
{
    public int First { get; set; }

    public int Second { get; set; }

    public List<string> Tags { get; } = new();
}

public class H24mBindingOrderTripleDto
{
    public int A { get; set; }

    public int B { get; set; }

    public int C { get; set; }

    public List<string> Tags { get; } = new();
}

public class MemberInitBindingOrderMaterializerTests
{
    [Fact]
    public void ReversedInitializerOrderBesideACollectionInitializerMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H24mBindingOrderDto { Second = r.Second, First = r.First, Tags = { "t" } })
            .ToList()
            .ConvertAll(d => d.First + "/" + d.Second + "/" + string.Join(",", d.Tags));

        List<string> actual = db.Table<H24mBindingOrderRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H24mBindingOrderDto { Second = r.Second, First = r.First, Tags = { "t" } })
            .ToList()
            .ConvertAll(d => d.First + "/" + d.Second + "/" + string.Join(",", d.Tags));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RotatedInitializerOrderBesideACollectionInitializerMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H24mBindingOrderTripleDto { C = r.Id, A = r.First, B = r.Second, Tags = { "t" } })
            .ToList()
            .ConvertAll(d => d.A + "/" + d.B + "/" + d.C);

        List<string> actual = db.Table<H24mBindingOrderRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H24mBindingOrderTripleDto { C = r.Id, A = r.First, B = r.Second, Tags = { "t" } })
            .ToList()
            .ConvertAll(d => d.A + "/" + d.B + "/" + d.C);

        Assert.Equal(expected, actual);
    }

    private static List<H24mBindingOrderRow> Rows()
    {
        return
        [
            new H24mBindingOrderRow { Id = 1, First = 10, Second = 20 },
            new H24mBindingOrderRow { Id = 2, First = 30, Second = 40 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H24mBindingOrderRow>().Schema.CreateTable();
        db.Table<H24mBindingOrderRow>().AddRange(Rows());
        return db;
    }
}
