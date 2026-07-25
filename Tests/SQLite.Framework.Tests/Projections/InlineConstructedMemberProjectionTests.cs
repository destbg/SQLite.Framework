using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class InlineConstructedMemberProjectionTests
{
    private static List<PairSourceRow> Rows()
    {
        return new List<PairSourceRow>
        {
            new PairSourceRow { Id = 1, A = 10, B = 20 },
            new PairSourceRow { Id = 2, A = 30, B = 40 },
        };
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<PairSourceRow>().Schema.CreateTable();
        db.Table<PairSourceRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void AnonymousTypeMemberProjectionReadsNamedMember()
    {
        using TestDatabase db = Seed();

        List<int> oracle = Rows().OrderBy(r => r.Id).Select(r => new { X = r.A, Y = r.B }.Y).ToList();
        List<int> actual = db.Table<PairSourceRow>().OrderBy(r => r.Id).Select(r => new { X = r.A, Y = r.B }.Y).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void AnonymousTypeComputedMemberProjectionReadsNamedMember()
    {
        using TestDatabase db = Seed();

        List<int> oracle = Rows().OrderBy(r => r.Id).Select(r => new { X = r.A, Y = r.B, Z = r.A + r.B }.Z).ToList();
        List<int> actual = db.Table<PairSourceRow>().OrderBy(r => r.Id).Select(r => new { X = r.A, Y = r.B, Z = r.A + r.B }.Z).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void MemberInitMemberProjectionReadsNamedMember()
    {
        using TestDatabase db = Seed();

        List<int> oracle = Rows().OrderBy(r => r.Id).Select(r => new PairView { X = r.A, Y = r.B }.Y).ToList();
        List<int> actual = db.Table<PairSourceRow>().OrderBy(r => r.Id).Select(r => new PairView { X = r.A, Y = r.B }.Y).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void KeyValuePairMemberProjectionReadsValue()
    {
        using TestDatabase db = Seed();

        List<int> oracle = Rows().OrderBy(r => r.Id).Select(r => new KeyValuePair<int, int>(r.A, r.B).Value).ToList();
        List<int> actual = db.Table<PairSourceRow>().OrderBy(r => r.Id).Select(r => new KeyValuePair<int, int>(r.A, r.B).Value).ToList();

        Assert.Equal(oracle, actual);
    }
}

public class PairSourceRow
{
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class PairView
{
    public int X { get; set; }

    public int Y { get; set; }
}
