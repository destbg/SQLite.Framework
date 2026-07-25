using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20SgJlDoc")]
public sealed class H20SgJlDoc
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public List<int> Numbers { get; set; } = [];
}

public sealed class H20SgJlInner
{
    public List<int> Doubled { get; set; } = [];
}

public sealed class H20SgJlOuter
{
    public int Id { get; set; }

    public H20SgJlInner? Inner { get; set; }
}

public sealed class H20SgJlFlat
{
    public int Id { get; set; }

    public List<int> Doubled { get; set; } = [];
}

[JsonSerializable(typeof(List<int>))]
internal partial class H20SgJlContext : JsonSerializerContext;

public class MemberInitJsonListChainParityTests
{
    private static List<H20SgJlDoc> Docs() =>
    [
        new H20SgJlDoc { Id = 1, Title = "t1", Numbers = [1, 2, 3] },
        new H20SgJlDoc { Id = 2, Title = "t2", Numbers = [] },
        new H20SgJlDoc { Id = 3, Title = "t3", Numbers = [7] },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new(b => b.AddJsonContext(H20SgJlContext.Default));
        db.Table<H20SgJlDoc>().Schema.CreateTable();
        db.Table<H20SgJlDoc>().AddRange(Docs());
        return db;
    }

    [Fact]
    public void FlatMemberInitJsonListChainMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<(int, List<int>)> expected = Docs()
            .OrderBy(r => r.Id)
            .Select(r => new H20SgJlFlat { Id = r.Id, Doubled = r.Numbers.Select(x => x * 2).ToList() })
            .Select(x => (x.Id, x.Doubled))
            .ToList();

        List<(int, List<int>)> actual = db.Table<H20SgJlDoc>()
            .OrderBy(r => r.Id)
            .Select(r => new H20SgJlFlat { Id = r.Id, Doubled = r.Numbers.Select(x => x * 2).ToList() })
            .AsEnumerable()
            .Select(x => (x.Id, x.Doubled))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedMemberInitJsonListChainMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<(int, List<int>)> expected = Docs()
            .OrderBy(r => r.Id)
            .Select(r => new H20SgJlOuter { Id = r.Id, Inner = new H20SgJlInner { Doubled = r.Numbers.Select(x => x * 2).ToList() } })
            .Select(x => (x.Id, x.Inner!.Doubled))
            .ToList();

        List<(int, List<int>)> actual = db.Table<H20SgJlDoc>()
            .OrderBy(r => r.Id)
            .Select(r => new H20SgJlOuter { Id = r.Id, Inner = new H20SgJlInner { Doubled = r.Numbers.Select(x => x * 2).ToList() } })
            .AsEnumerable()
            .Select(x => (x.Id, x.Inner!.Doubled))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedMemberInitFilteredJsonListChainMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<(int, List<int>)> expected = Docs()
            .OrderBy(r => r.Id)
            .Select(r => new H20SgJlOuter { Id = r.Id, Inner = new H20SgJlInner { Doubled = r.Numbers.Where(x => x > 1).Select(x => x * 2).ToList() } })
            .Select(x => (x.Id, x.Inner!.Doubled))
            .ToList();

        List<(int, List<int>)> actual = db.Table<H20SgJlDoc>()
            .OrderBy(r => r.Id)
            .Select(r => new H20SgJlOuter { Id = r.Id, Inner = new H20SgJlInner { Doubled = r.Numbers.Where(x => x > 1).Select(x => x * 2).ToList() } })
            .AsEnumerable()
            .Select(x => (x.Id, x.Inner!.Doubled))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
