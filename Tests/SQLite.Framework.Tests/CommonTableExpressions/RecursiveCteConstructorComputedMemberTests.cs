using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26bChainSeeds")]
public class H26bChainSeed
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H26bChainSide
{
    public H26bChainSide(int x)
    {
        X = x;
        Doubled = x * 2;
    }

    public int X { get; set; }

    public int Doubled { get; set; }
}

public class H26bChainNode
{
    public int Id { get; set; }

    public H26bChainSide? Side { get; set; }
}

public class RecursiveCteConstructorComputedMemberTests
{
    [Fact]
    public void AConstructorComputedNestedMemberKeepsItsValueThroughARecursiveCommonTableExpression()
    {
        using TestDatabase db = Setup(nameof(AConstructorComputedNestedMemberKeepsItsValueThroughARecursiveCommonTableExpression));

        List<int> expected = Expand().Select(n => n.Side!.Doubled).OrderBy(v => v).ToList();

        Assert.Equal(new List<int> { 10, 22, 24 }, expected);

        SQLiteCte<H26bChainNode> cte = db.WithRecursive<H26bChainNode>(self =>
            db.Table<H26bChainSeed>()
                .Where(r => r.Id == 1)
                .Select(r => new H26bChainNode { Id = r.Id, Side = new H26bChainSide(r.A) })
                .Concat(from s in self
                        where s.Id < 3
                        select new H26bChainNode { Id = s.Id + 1, Side = new H26bChainSide(s.Id + 10) }));

        List<int> actual = cte
            .Select(x => x.Side!.Doubled)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26bChainNode> Expand()
    {
        H26bChainSeed seed = Rows().Single(r => r.Id == 1);
        List<H26bChainNode> nodes = [new H26bChainNode { Id = seed.Id, Side = new H26bChainSide(seed.A) }];
        int id = seed.Id;
        while (id < 3)
        {
            nodes.Add(new H26bChainNode { Id = id + 1, Side = new H26bChainSide(id + 10) });
            id += 1;
        }

        return nodes;
    }

    private static List<H26bChainSeed> Rows()
    {
        return
        [
            new H26bChainSeed { Id = 1, A = 5 },
            new H26bChainSeed { Id = 2, A = 20 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26bChainSeed>().Schema.CreateTable();
        db.Table<H26bChainSeed>().AddRange(Rows());
        return db;
    }
}
