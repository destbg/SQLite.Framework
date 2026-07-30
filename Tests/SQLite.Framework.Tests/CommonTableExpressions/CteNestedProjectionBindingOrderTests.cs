using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25dNestRows")]
public class H25dNestRow
{
    [Key]
    public int Id { get; set; }

    public int Link { get; set; }

    public int A { get; set; }

    public int B { get; set; }

    public int V { get; set; }
}

public class H25dNestPart
{
    public int First { get; set; }

    public int Second { get; set; }
}

public class CteNestedProjectionBindingOrderTests
{
    [Fact]
    public void NestedComputedMembersWrittenOutOfDeclarationOrderKeepTheirOwnValues()
    {
        using TestDatabase db = Setup(nameof(NestedComputedMembersWrittenOutOfDeclarationOrderKeepTheirOwnValues));

        List<(int Id, int First, int Second)> expected = Rows()
            .Select(r => new { r.Id, Part = new H25dNestPart { Second = r.B * 3, First = r.A * 2 } })
            .Select(x => (x.Id, x.Part.First, x.Part.Second))
            .OrderBy(t => t.Id)
            .ToList();

        List<(int Id, int First, int Second)> actual = db.With(() => db.Table<H25dNestRow>()
                .Select(r => new { r.Id, Part = new H25dNestPart { Second = r.B * 3, First = r.A * 2 } }))
            .Select(x => new { x.Id, x.Part.First, x.Part.Second })
            .AsEnumerable()
            .Select(x => (x.Id, x.First, x.Second))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedJoinMembersWrittenOutOfDeclarationOrderBesideAnArrayMemberKeepTheirOwnValues()
    {
        using TestDatabase db = Setup(nameof(NestedJoinMembersWrittenOutOfDeclarationOrderBesideAnArrayMemberKeepTheirOwnValues));

        List<(int First, int Second)> expected = (
                from a in Rows()
                join b in Rows() on a.Link equals b.Id
                where a.Id > 0
                select new { Part = new H25dNestPart { Second = b.V, First = a.V }, Tags = new[] { a.Link } })
            .Select(x => (x.Part.First, x.Part.Second))
            .OrderBy(t => t.First)
            .ThenBy(t => t.Second)
            .ToList();

        List<(int First, int Second)> actual = db.With(() =>
                from a in db.Table<H25dNestRow>()
                join b in db.Table<H25dNestRow>() on a.Link equals b.Id
                where a.Id > 0
                select new { Part = new H25dNestPart { Second = b.V, First = a.V }, Tags = new[] { a.Link } })
            .Select(x => new { x.Part.First, x.Part.Second })
            .AsEnumerable()
            .Select(x => (x.First, x.Second))
            .OrderBy(t => t.First)
            .ThenBy(t => t.Second)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25dNestRow> Rows()
    {
        return
        [
            new H25dNestRow { Id = 1, Link = 2, A = 10, B = 100, V = 11 },
            new H25dNestRow { Id = 2, Link = 2, A = 20, B = 200, V = 44 },
            new H25dNestRow { Id = 3, Link = 2, A = 30, B = 300, V = 99 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25dNestRow>().Schema.CreateTable();
        db.Table<H25dNestRow>().AddRange(Rows());
        return db;
    }
}
