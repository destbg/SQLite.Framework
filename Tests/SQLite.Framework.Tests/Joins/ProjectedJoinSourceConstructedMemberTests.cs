using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26aJoinLefts")]
public class H26aJoinLeft
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

[Table("H26aJoinRights")]
public class H26aJoinRight
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }

    public int A { get; set; }

    public int? B { get; set; }
}

public class H26aJoinPart
{
    public int P { get; set; }

    public int Q { get; set; }
}

public class H26aJoinNullPart
{
    public int? N { get; set; }
}

public class H26aJoinOuter
{
    public int K { get; set; }

    public H26aJoinPart? Part { get; set; }
}

public class H26aJoinNullOuter
{
    public int K { get; set; }

    public H26aJoinNullPart? Part { get; set; }
}

public class ProjectedJoinSourceConstructedMemberTests
{
    [Fact]
    public void AnUnsetNestedMemberOfAProjectedJoinSourceReadsItsDefault()
    {
        using TestDatabase db = Setup(nameof(AnUnsetNestedMemberOfAProjectedJoinSourceReadsItsDefault));

        List<int> expected = Lefts()
            .Join(
                Rights().Select(r => new H26aJoinOuter { K = r.K, Part = new H26aJoinPart { P = r.A } }),
                l => l.K,
                o => o.K,
                (l, o) => o.Part!.Q)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(new List<int> { 0, 0 }, expected);

        List<int> actual = db.Table<H26aJoinLeft>()
            .Join(
                db.Table<H26aJoinRight>().Select(r => new H26aJoinOuter { K = r.K, Part = new H26aJoinPart { P = r.A } }),
                l => l.K,
                o => o.K,
                (l, o) => o.Part!.Q)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ANestedObjectOfAProjectedJoinSourceStaysNonNullWhenEveryColumnIsNull()
    {
        using TestDatabase db = Setup(nameof(ANestedObjectOfAProjectedJoinSourceStaysNonNullWhenEveryColumnIsNull));

        List<bool> expected = Lefts()
            .Join(
                Rights().Select(r => new H26aJoinNullOuter { K = r.K, Part = new H26aJoinNullPart { N = r.B } }),
                l => l.K,
                o => o.K,
                (l, o) => o)
            .OrderBy(o => o.K)
            .Select(o => o.Part != null)
            .ToList();

        Assert.Equal(new List<bool> { true, true }, expected);

        List<bool> actual = db.Table<H26aJoinLeft>()
            .Join(
                db.Table<H26aJoinRight>().Select(r => new H26aJoinNullOuter { K = r.K, Part = new H26aJoinNullPart { N = r.B } }),
                l => l.K,
                o => o.K,
                (l, o) => o)
            .AsEnumerable()
            .OrderBy(o => o.K)
            .Select(o => o.Part != null)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26aJoinLeft> Lefts()
    {
        return
        [
            new H26aJoinLeft { Id = 1, K = 1 },
            new H26aJoinLeft { Id = 2, K = 2 }
        ];
    }

    private static List<H26aJoinRight> Rights()
    {
        return
        [
            new H26aJoinRight { Id = 1, K = 1, A = 5, B = null },
            new H26aJoinRight { Id = 2, K = 2, A = 6, B = null }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26aJoinLeft>().Schema.CreateTable();
        db.Table<H26aJoinRight>().Schema.CreateTable();
        db.Table<H26aJoinLeft>().AddRange(Lefts());
        db.Table<H26aJoinRight>().AddRange(Rights());
        return db;
    }
}
