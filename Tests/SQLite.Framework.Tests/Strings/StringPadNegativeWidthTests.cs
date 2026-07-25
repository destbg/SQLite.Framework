using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringPadNegativeWidthTests
{
    [Fact]
    public void PadLeftNegativeWidthReturnsOriginal()
    {
        using TestDatabase db = Seed("abcdef");

        Assert.Throws<ArgumentOutOfRangeException>(() => "abcdef".PadLeft(-3));

        string actual = db.Table<TwoStringEntity>().Select(x => x.A.PadLeft(-3)).Single();

        Assert.Equal("abcdef", actual);
    }

    [Fact]
    public void PadRightNegativeWidthReturnsOriginal()
    {
        using TestDatabase db = Seed("abcdef");

        Assert.Throws<ArgumentOutOfRangeException>(() => "abcdef".PadRight(-3));

        string actual = db.Table<TwoStringEntity>().Select(x => x.A.PadRight(-3)).Single();

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
