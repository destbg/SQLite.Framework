using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ScalarRecursiveCteParityTests
{
    [Fact]
    public void ScalarRecursiveCte_ProducesSequence()
    {
        using TestDatabase db = new();
        SQLiteCte<int> cte = db.WithRecursive<int>(self =>
            db.Values(1).Concat(from x in self where x < 10 select x + 1));

        List<int> expected = Enumerable.Range(1, 10).ToList();
        List<int> actual = (from x in cte select x).OrderBy(x => x).ToList();
        Assert.Equal(expected, actual);
    }
}
