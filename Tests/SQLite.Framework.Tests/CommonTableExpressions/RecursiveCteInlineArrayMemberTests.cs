using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21nArrWalks")]
public class H21nArrWalk
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H21nArrWalkStep
{
    public int Id { get; set; }

    public int[] Tags { get; set; } = [];
}

public class RecursiveCteInlineArrayMemberTests
{
    private static List<H21nArrWalk> Rows()
    {
        return
        [
            new H21nArrWalk { Id = 1, A = 10 },
            new H21nArrWalk { Id = 2, A = 20 },
            new H21nArrWalk { Id = 3, A = 30 },
            new H21nArrWalk { Id = 4, A = 40 },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21nArrWalk>().Schema.CreateTable();
        db.Table<H21nArrWalk>().AddRange(Rows());
        return db;
    }

    private static List<int> ExpectedIds(List<H21nArrWalk> rows)
    {
        List<int> reached = [];
        List<int> frontier = rows.Where(w => w.Id == 1).Select(w => w.Id).ToList();

        while (frontier.Count > 0)
        {
            reached.AddRange(frontier);
            frontier = frontier.Where(id => id < 4).Select(id => id + 1).ToList();
        }

        return reached.OrderBy(i => i).ToList();
    }

    [Fact]
    public void RecursiveBodyWithoutArrayMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = ExpectedIds(Rows());

        SQLiteCte<H21nArrWalkStep> cte = db.WithRecursive<H21nArrWalkStep>(self =>
            db.Table<H21nArrWalk>()
                .Where(w => w.Id == 1)
                .Select(w => new H21nArrWalkStep { Id = w.Id })
                .Concat(from s in self
                        where s.Id < 4
                        select new H21nArrWalkStep { Id = s.Id + 1 }));

        List<int> actual = cte.Select(s => s.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RecursiveBodyWithArrayMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = ExpectedIds(Rows());

        SQLiteCte<H21nArrWalkStep> cte = db.WithRecursive<H21nArrWalkStep>(self =>
            db.Table<H21nArrWalk>()
                .Where(w => w.Id == 1)
                .Select(w => new H21nArrWalkStep { Id = w.Id, Tags = new[] { w.A } })
                .Concat(from s in self
                        where s.Id < 4
                        select new H21nArrWalkStep { Id = s.Id + 1, Tags = new[] { s.Id } }));

        List<int> actual = cte.Select(s => s.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }
}
