using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TupleCreateNullableArgumentTests
{
    private static List<NullableTupleRow> Rows()
    {
        return new List<NullableTupleRow>
        {
            new NullableTupleRow { Id = 1, A = 5, N = 7 },
            new NullableTupleRow { Id = 2, A = 9, N = null },
        };
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<NullableTupleRow>().Schema.CreateTable();
        db.Table<NullableTupleRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void TupleCreateWithNullableArgumentReadsItem1()
    {
        using TestDatabase db = Seed();

        List<int> oracle = Rows().OrderBy(r => r.Id).Select(r => Tuple.Create(r.A, r.N).Item1).ToList();
        List<int> actual = db.Table<NullableTupleRow>().OrderBy(r => r.Id).Select(r => Tuple.Create(r.A, r.N).Item1).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ValueTupleCreateWithNullableArgumentReadsItem2()
    {
        using TestDatabase db = Seed();

        List<int?> oracle = Rows().OrderBy(r => r.Id).Select(r => ValueTuple.Create(r.Id, r.N).Item2).ToList();
        List<int?> actual = db.Table<NullableTupleRow>().OrderBy(r => r.Id).Select(r => ValueTuple.Create(r.Id, r.N).Item2).ToList();

        Assert.Equal(oracle, actual);
    }
}

public class NullableTupleRow
{
    public int Id { get; set; }

    public int A { get; set; }

    public int? N { get; set; }
}
