using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringCultureAwareContainsTests
{
    [Fact]
    public void ContainsInvariantCultureComparesBytewise()
    {
        string decomposed = "cafe\u0301";
        string precomposed = "caf\u00e9";
        using TestDatabase db = Seed(decomposed, precomposed);

        Assert.True(decomposed.Contains(precomposed, StringComparison.InvariantCulture));

        bool actual = db.Table<TwoStringEntity>()
            .Select(x => x.A.Contains(x.B, StringComparison.InvariantCulture))
            .Single();

        Assert.False(actual);
    }

    [Fact]
    public void StartsWithInvariantCultureComparesBytewise()
    {
        string decomposed = "cafe\u0301bar";
        string precomposed = "caf\u00e9";
        using TestDatabase db = Seed(decomposed, precomposed);

        Assert.True(decomposed.StartsWith(precomposed, StringComparison.InvariantCulture));

        bool actual = db.Table<TwoStringEntity>()
            .Select(x => x.A.StartsWith(x.B, StringComparison.InvariantCulture))
            .Single();

        Assert.False(actual);
    }

    private static TestDatabase Seed(string a, string b)
    {
        TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().Add(new TwoStringEntity { Id = 1, A = a, B = b });
        return db;
    }
}
