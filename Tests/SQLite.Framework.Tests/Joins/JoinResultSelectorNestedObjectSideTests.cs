using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26aSideRows")]
public class H26aSideRow
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class H26aSidePart
{
    public int P { get; set; }
}

public class H26aSideOuter
{
    public int K { get; set; }

    public H26aSidePart? Part { get; set; }
}

public class JoinResultSelectorNestedObjectSideTests
{
    [Fact]
    public void AJoinResultSelectorReadsTheNestedObjectOfTheInnerSource()
    {
        using TestDatabase db = Setup(nameof(AJoinResultSelectorReadsTheNestedObjectOfTheInnerSource));

        List<int> expected = Rows()
            .Select(r => new H26aSideOuter { K = r.K, Part = new H26aSidePart { P = r.A } })
            .Join(
                Rows().Select(r => new H26aSideOuter { K = r.K, Part = new H26aSidePart { P = r.B } }),
                o => o.K,
                p => p.K,
                (o, p) => p.Part!)
            .Select(part => part.P)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(new List<int> { 50, 60 }, expected);

        List<int> actual = db.Table<H26aSideRow>()
            .Select(r => new H26aSideOuter { K = r.K, Part = new H26aSidePart { P = r.A } })
            .Join(
                db.Table<H26aSideRow>().Select(r => new H26aSideOuter { K = r.K, Part = new H26aSidePart { P = r.B } }),
                o => o.K,
                p => p.K,
                (o, p) => p.Part!)
            .AsEnumerable()
            .Select(part => part.P)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26aSideRow> Rows()
    {
        return
        [
            new H26aSideRow { Id = 1, K = 1, A = 5, B = 50 },
            new H26aSideRow { Id = 2, K = 2, A = 6, B = 60 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26aSideRow>().Schema.CreateTable();
        db.Table<H26aSideRow>().AddRange(Rows());
        return db;
    }
}
