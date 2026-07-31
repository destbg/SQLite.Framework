using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26aGroupedRows")]
public class H26aGroupedRow
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }

    public int A { get; set; }
}

public class H26aGroupedPart
{
    public int P { get; set; }

    public int Q { get; set; }
}

public class H26aGroupedOuter
{
    public int K { get; set; }

    public H26aGroupedPart? Part { get; set; }
}

public class GroupedElementConstructedMemberTests
{
    [Fact]
    public void AGroupedSumOverAnUnsetNestedMemberUsesItsDefault()
    {
        using TestDatabase db = Setup(nameof(AGroupedSumOverAnUnsetNestedMemberUsesItsDefault));

        List<int> expected = Rows()
            .Select(r => new H26aGroupedOuter { K = r.K, Part = new H26aGroupedPart { P = r.A } })
            .GroupBy(x => x.K)
            .Select(g => g.Sum(e => e.Part!.Q))
            .ToList();

        Assert.Equal(new List<int> { 0 }, expected);

        List<int> actual = db.Table<H26aGroupedRow>()
            .Select(r => new H26aGroupedOuter { K = r.K, Part = new H26aGroupedPart { P = r.A } })
            .GroupBy(x => x.K)
            .Select(g => g.Sum(e => e.Part!.Q))
            .AsEnumerable()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AGroupedMaxOverAnUnsetNestedMemberUsesItsDefault()
    {
        using TestDatabase db = Setup(nameof(AGroupedMaxOverAnUnsetNestedMemberUsesItsDefault));

        List<int> expected = Rows()
            .Select(r => new H26aGroupedOuter { K = r.K, Part = new H26aGroupedPart { P = r.A } })
            .GroupBy(x => x.K)
            .Select(g => g.Max(e => e.Part!.Q))
            .ToList();

        Assert.Equal(new List<int> { 0 }, expected);

        List<int> actual = db.Table<H26aGroupedRow>()
            .Select(r => new H26aGroupedOuter { K = r.K, Part = new H26aGroupedPart { P = r.A } })
            .GroupBy(x => x.K)
            .Select(g => g.Max(e => e.Part!.Q))
            .AsEnumerable()
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26aGroupedRow> Rows()
    {
        return
        [
            new H26aGroupedRow { Id = 1, K = 7, A = 5 },
            new H26aGroupedRow { Id = 2, K = 7, A = 20 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26aGroupedRow>().Schema.CreateTable();
        db.Table<H26aGroupedRow>().AddRange(Rows());
        return db;
    }
}
