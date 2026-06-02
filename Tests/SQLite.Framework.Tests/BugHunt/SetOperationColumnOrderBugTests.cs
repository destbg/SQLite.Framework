using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal sealed class OrderedPair
{
    public int A { get; set; }

    public int B { get; set; }
}

public class SetOperationColumnOrderBugTests
{
    [Fact]
    public void RecursiveCteWithSwappedBindingOrder_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        SQLiteCte<OrderedPair> cte = db.WithRecursive<OrderedPair>(self =>
            db.Values(new OrderedPair { A = 0, B = 100 })
                .Concat(from p in self
                    where p.A < 3
                    select new OrderedPair { B = p.B + 1, A = p.A + 1 }));

        List<(int A, int B)> actual = (from p in cte select p)
            .ToList()
            .Select(p => (p.A, p.B))
            .OrderBy(t => t.A)
            .ToList();

        List<(int A, int B)> expected = [];
        int a = 0;
        int b = 100;
        expected.Add((a, b));
        while (a < 3)
        {
            (a, b) = (a + 1, b + 1);
            expected.Add((a, b));
        }

        Assert.Equal(expected, actual);
    }
}
