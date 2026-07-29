using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CibRows")]
public class CibRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class CibFieldListDto
{
    public List<int> Items = [];
}

public class CibInnerListDto
{
    public List<int> Items { get; set; } = [];
}

public class CibOuterDto
{
    public int Id { get; set; }

    public CibInnerListDto Inner { get; set; } = new();
}

public class CollectionInitializerBindingShapeTests
{
    [Fact]
    public void AListInitializerOnAFieldReadsItsValues()
    {
        using TestDatabase db = Seed();

        List<List<int>> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new CibFieldListDto { Items = { r.Id, r.A } })
            .Select(d => d.Items)
            .ToList();

        List<List<int>> actual = db.Table<CibRow>().OrderBy(r => r.Id)
            .Select(r => new CibFieldListDto { Items = { r.Id, r.A } })
            .AsEnumerable()
            .Select(d => d.Items)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AListInitializerInsideANestedMemberReadsItsValues()
    {
        using TestDatabase db = Seed();

        List<List<int>> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new CibOuterDto { Id = r.Id, Inner = new CibInnerListDto { Items = { r.A } } })
            .Select(d => d.Inner.Items)
            .ToList();

        List<List<int>> actual = db.Table<CibRow>().OrderBy(r => r.Id)
            .Select(r => new CibOuterDto { Id = r.Id, Inner = new CibInnerListDto { Items = { r.A } } })
            .AsEnumerable()
            .Select(d => d.Inner.Items)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<CibRow> Rows()
    {
        return
        [
            new CibRow { Id = 1, A = 5 },
            new CibRow { Id = 2, A = 6 }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<CibRow>().Schema.CreateTable();
        db.Table<CibRow>().AddRange(Rows());
        return db;
    }
}
