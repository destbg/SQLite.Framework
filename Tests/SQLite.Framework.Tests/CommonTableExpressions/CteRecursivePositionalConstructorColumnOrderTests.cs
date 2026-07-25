using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CteRecursivePositionalConstructorColumnOrderTests
{
    [Fact]
    public void RecursiveTermPositionalConstructor_MisalignsColumnsWhenParamOrderDiffersFromProperties()
    {
        using TestDatabase db = new();

        SQLiteCte<Step> cte = db.WithRecursive<Step>(self =>
            db.Values(new Step { Value = 1, Next = 100 })
                .Concat(from s in self
                        where s.Value < 3
                        select new Step(s.Next - 1, s.Value + 1)));

        List<(int Value, int Next)> actual = (from c in cte
                                              orderby c.Value
                                              select new { c.Value, c.Next })
            .ToList()
            .Select(c => (c.Value, c.Next))
            .ToList();

        Assert.Equal(new[] { (1, 100), (99, 2) }, actual.ToArray());
    }

    [Fact]
    public void RecursiveTermMemberInit_KeepsColumnsAlignedWithAnchor()
    {
        using TestDatabase db = new();

        SQLiteCte<Step> cte = db.WithRecursive<Step>(self =>
            db.Values(new Step { Value = 1, Next = 100 })
                .Concat(from s in self
                        where s.Value < 3
                        select new Step { Value = s.Value + 1, Next = s.Next - 1 }));

        List<(int Value, int Next)> actual = (from c in cte
                                              orderby c.Value
                                              select new { c.Value, c.Next })
            .ToList()
            .Select(c => (c.Value, c.Next))
            .ToList();

        Assert.Equal(new[] { (1, 100), (2, 99), (3, 98) }, actual.ToArray());
    }

    private class Step
    {
        public Step()
        {
        }

        public Step(int next, int value)
        {
            Next = next;
            Value = value;
        }

        public int Value { get; set; }
        public int Next { get; set; }
    }
}
