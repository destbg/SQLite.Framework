using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TupleArityEntityMaterializationTests
{
    [Fact]
    public void TupleOfThreeIsMaterializedFromRawQuery()
    {
        using TestDatabase db = new();

        Tuple<int, string, double> actual = db.Query<Tuple<int, string, double>>(
            "SELECT 1 AS \"Item1\", 'x' AS \"Item2\", 2.5 AS \"Item3\"")[0];

        Assert.Equal(Tuple.Create(1, "x", 2.5), actual);
    }

    [Fact]
    public void TupleOfFourIsMaterializedFromRawQuery()
    {
        using TestDatabase db = new();

        Tuple<int, string, double, long> actual = db.Query<Tuple<int, string, double, long>>(
            "SELECT 1 AS \"Item1\", 'x' AS \"Item2\", 2.5 AS \"Item3\", 9 AS \"Item4\"")[0];

        Assert.Equal(Tuple.Create(1, "x", 2.5, 9L), actual);
    }

    [Fact]
    public void TupleOfSevenIsMaterializedFromRawQuery()
    {
        using TestDatabase db = new();

        Tuple<int, int, int, int, int, int, int> actual = db.Query<Tuple<int, int, int, int, int, int, int>>(
            "SELECT 1 AS \"Item1\", 2 AS \"Item2\", 3 AS \"Item3\", 4 AS \"Item4\", 5 AS \"Item5\", 6 AS \"Item6\", 7 AS \"Item7\"")[0];

        Assert.Equal(Tuple.Create(1, 2, 3, 4, 5, 6, 7), actual);
    }
}
