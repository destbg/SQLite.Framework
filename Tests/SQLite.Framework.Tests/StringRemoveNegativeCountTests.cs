using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringRemoveNegativeCountTests
{
    [Fact]
    public void RemoveNegativeCountRemovesNothing()
    {
        using TestDatabase db = Seed("abcdef");

        Assert.Throws<ArgumentOutOfRangeException>(() => "abcdef".Remove(2, -2));

        string actual = db.Table<TwoStringEntity>().Select(x => x.A.Remove(2, -2)).Single();

        Assert.Equal("abcdef", actual);
    }

    private static TestDatabase Seed(string a)
    {
        TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().Add(new TwoStringEntity { Id = 1, A = a, B = "" });
        return db;
    }
}
