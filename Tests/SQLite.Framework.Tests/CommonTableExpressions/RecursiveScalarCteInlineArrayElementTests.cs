using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22qWalkSeeds")]
public class H22qWalkSeed
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class RecursiveScalarCteInlineArrayElementTests
{
    [Fact]
    public void RecursiveScalarCteWithInlineArrayAnchorMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Expected(Rows());

        SQLiteCte<int> cte = db.WithRecursive<int>(self =>
            db.Table<H22qWalkSeed>()
                .Where(r => r.Id == 1)
                .Select(r => new[] { r.A, r.B }[0])
                .Concat(from x in self
                        where x < 5
                        select x + 1));

        List<int> actual = cte
            .Select(v => v)
            .ToList()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22qWalkSeed> Rows()
    {
        return
        [
            new H22qWalkSeed { Id = 1, A = 1, B = 100 },
            new H22qWalkSeed { Id = 2, A = 7, B = 200 }
        ];
    }

    private static List<int> Expected(List<H22qWalkSeed> rows)
    {
        List<int> reached = [];
        List<int> frontier = rows
            .Where(r => r.Id == 1)
            .Select(r => new[] { r.A, r.B }[0])
            .ToList();

        while (frontier.Count > 0)
        {
            reached.AddRange(frontier);
            frontier = frontier.Where(x => x < 5).Select(x => x + 1).ToList();
        }

        return reached.OrderBy(v => v).ToList();
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22qWalkSeed>().Schema.CreateTable();
        db.Table<H22qWalkSeed>().AddRange(Rows());
        return db;
    }
}
