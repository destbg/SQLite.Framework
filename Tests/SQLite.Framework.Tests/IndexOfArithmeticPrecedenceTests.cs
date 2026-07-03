using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class IndexOfArithmeticPrecedenceTests
{
    [Fact]
    public void IndexOfMultipliedKeepsPrecedence()
    {
        using TestDatabase db = Seed("abc");

        int expected = "abc".IndexOf('b') * 2;
        Assert.Equal(2, expected);

        int actual = db.Table<TwoStringEntity>().Select(x => x.A.IndexOf('b') * 2).Single();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexOfAsRightSubtractOperandKeepsPrecedence()
    {
        using TestDatabase db = Seed("abc");

        int expected = 10 - "abc".IndexOf('b');
        Assert.Equal(9, expected);

        int actual = db.Table<TwoStringEntity>().Select(x => 10 - x.A.IndexOf('b')).Single();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Seed(string a)
    {
        TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().Add(new TwoStringEntity { Id = 1, A = a, B = "" });
        return db;
    }
}
