using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25cSpanRows")]
public class H25cSpanRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

[Table("H25cPairRows")]
public class H25cPairRow
{
    [Key]
    public int A { get; set; }

    [Key]
    public int B { get; set; }

    public int V { get; set; }
}

public class H25cSpanPart
{
    public H25cSpanPart(int value)
    {
        Value = value;
    }

    public int Value { get; set; }
}

public class WholeRowGroupKeyTests
{
    [Fact]
    public void GroupingAProjectedRowWithAnInMemoryMemberByItselfReportsAClearMessage()
    {
        using TestDatabase db = SetupSpans(nameof(GroupingAProjectedRowWithAnInMemoryMemberByItselfReportsAClearMessage));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<H25cSpanRow>()
            .Select(r => new { r.Id, Part = new H25cSpanPart(r.A) })
            .GroupBy(x => x)
            .Select(g => g.Count())
            .ToList());

        Assert.Contains("computed in memory", ex.Message);
    }

    [Fact]
    public void GroupingAProjectedRowByItselfCountsEveryProjectedColumnCombination()
    {
        using TestDatabase db = SetupSpans(nameof(GroupingAProjectedRowByItselfCountsEveryProjectedColumnCombination));

        List<int> expected = SpanRows()
            .Select(r => new { r.A, r.B })
            .GroupBy(x => x)
            .Select(g => g.Count())
            .OrderBy(c => c)
            .ToList();

        List<int> actual = db.Table<H25cSpanRow>()
            .Select(r => new { r.A, r.B })
            .GroupBy(x => x)
            .Select(g => g.Count())
            .ToList()
            .OrderBy(c => c)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupingAProjectedRowByItselfReturnsEveryDistinctKeyPair()
    {
        using TestDatabase db = SetupSpans(nameof(GroupingAProjectedRowByItselfReturnsEveryDistinctKeyPair));

        List<(int A, int B)> expected = SpanRows()
            .Select(r => new { r.A, r.B })
            .GroupBy(x => x)
            .Select(g => (A: g.Key.A, B: g.Key.B))
            .OrderBy(x => x.A)
            .ThenBy(x => x.B)
            .ToList();

        List<(int A, int B)> actual = db.Table<H25cSpanRow>()
            .Select(r => new { r.A, r.B })
            .GroupBy(x => x)
            .Select(g => new { g.Key.A, g.Key.B })
            .ToList()
            .Select(x => (A: x.A, B: x.B))
            .OrderBy(x => x.A)
            .ThenBy(x => x.B)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupingAnEntityByItselfUsesEveryPrimaryKeyColumn()
    {
        using TestDatabase db = SetupPairs(nameof(GroupingAnEntityByItselfUsesEveryPrimaryKeyColumn));

        List<int> expected = PairRows()
            .GroupBy(r => r)
            .Select(g => g.Count())
            .OrderBy(c => c)
            .ToList();

        List<int> actual = db.Table<H25cPairRow>()
            .GroupBy(r => r)
            .Select(g => g.Count())
            .ToList()
            .OrderBy(c => c)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25cSpanRow> SpanRows()
    {
        return
        [
            new H25cSpanRow { Id = 1, A = 1, B = 1 },
            new H25cSpanRow { Id = 2, A = 1, B = 2 },
            new H25cSpanRow { Id = 3, A = 2, B = 1 }
        ];
    }

    private static List<H25cPairRow> PairRows()
    {
        return
        [
            new H25cPairRow { A = 1, B = 1, V = 10 },
            new H25cPairRow { A = 1, B = 2, V = 20 },
            new H25cPairRow { A = 2, B = 1, V = 30 }
        ];
    }

    private static TestDatabase SetupSpans(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25cSpanRow>().Schema.CreateTable();
        db.Table<H25cSpanRow>().AddRange(SpanRows());
        return db;
    }

    private static TestDatabase SetupPairs(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25cPairRow>().Schema.CreateTable();
        db.Table<H25cPairRow>().AddRange(PairRows());
        return db;
    }
}
