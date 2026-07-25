using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class HopPart
{
    public HopPart(int p)
    {
        P = p;
    }

    public int P { get; }
}

public class HopNode
{
    public HopNode(HopPart part)
    {
        Part = part;
    }

    public HopPart Part { get; }

    public int B { get; set; }

    public int C { get; set; }
}

public class CteRecursiveMemberInitNestedCtorTests
{
    [Fact]
    public void RecursiveArmMemberInitOrderStaysAlignedWithTheAnchor()
    {
        using TestDatabase db = new();
        db.Table<CteSwapBookRow>().Schema.CreateTable();
        db.Table<CteSwapBookRow>().Add(new CteSwapBookRow { Id = 1, AuthorId = 7 });

        SQLiteCte<HopNode> cte = db.WithRecursive<HopNode>(self =>
            db.Table<CteSwapBookRow>()
                .Select(r => new HopNode(new HopPart(r.Id)) { B = r.Id, C = r.Id + 10 })
                .Concat(from s in self
                        where s.B < 3
                        select new HopNode(new HopPart(s.Part.P)) { C = s.C, B = s.B + 1 }));

        List<(int B, int C)> actual = (from c in cte
                                       orderby c.B
                                       select new { c.B, c.C })
            .ToList()
            .Select(c => (c.B, c.C))
            .ToList();

        Assert.Equal([(1, 11), (2, 11), (3, 11)], actual);
    }
}
