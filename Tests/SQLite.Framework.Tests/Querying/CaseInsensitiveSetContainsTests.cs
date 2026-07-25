using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CaseInsensitiveSetContainsTests
{
    [Fact]
    public void HashSetContainsIgnoresSetComparerAndComparesCaseSensitively()
    {
        using TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().AddRange(
        [
            new TwoStringEntity { Id = 1, A = "ALPHA", B = "" },
            new TwoStringEntity { Id = 2, A = "beta", B = "" },
        ]);

        HashSet<string> set = new(StringComparer.OrdinalIgnoreCase) { "alpha" };

        List<int> inMemory = new[]
            {
                new TwoStringEntity { Id = 1, A = "ALPHA", B = "" },
                new TwoStringEntity { Id = 2, A = "beta", B = "" },
            }
            .Where(x => set.Contains(x.A))
            .Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([1], inMemory);

        List<int> actual = db.Table<TwoStringEntity>()
            .Where(x => set.Contains(x.A))
            .Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([], actual);
    }
}
