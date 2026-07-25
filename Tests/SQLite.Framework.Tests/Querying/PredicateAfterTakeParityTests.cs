using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PredicateAfterTakeParityTests
{
    public class TakeRow
    {
        [Key]
        public int Id { get; set; }
        public int V { get; set; }
    }

    private static readonly TakeRow[] Seed =
    [
        new TakeRow { Id = 1, V = 1 },
        new TakeRow { Id = 2, V = 2 },
        new TakeRow { Id = 3, V = 3 },
        new TakeRow { Id = 4, V = 4 },
        new TakeRow { Id = 5, V = 5 },
    ];

    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<TakeRow>().Schema.CreateTable();
        foreach (TakeRow r in Seed)
        {
            db.Table<TakeRow>().Add(r);
        }
        return db;
    }

    [Fact]
    public void FirstOrDefaultPredicateAfterTake_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        int oracle = Seed.OrderBy(x => x.Id).Take(3).Select(x => x.Id).FirstOrDefault(id => id == 5);
        int actual = db.Table<TakeRow>().OrderBy(x => x.Id).Take(3).Select(x => x.Id).FirstOrDefault(id => id == 5);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SingleOrDefaultPredicateAfterTake_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        int oracle = Seed.OrderBy(x => x.Id).Take(2).Select(x => x.Id).SingleOrDefault(id => id == 4);
        int actual = db.Table<TakeRow>().OrderBy(x => x.Id).Take(2).Select(x => x.Id).SingleOrDefault(id => id == 4);

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SingleWithoutPredicateAfterTake_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        int oracle = Seed.OrderBy(x => x.Id).Take(1).Select(x => x.Id).Single();
        int actual = db.Table<TakeRow>().OrderBy(x => x.Id).Take(1).Select(x => x.Id).Single();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void FirstPredicateAfterTakeWithNoMatchInWindow_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        Func<IQueryable<TakeRow>, int> query = q => q.OrderBy(x => x.Id).Take(2).Select(x => x.Id).First(id => id == 5);

        Assert.Throws<InvalidOperationException>(() => query(Seed.AsQueryable()));
        Assert.Throws<InvalidOperationException>(() => query(db.Table<TakeRow>()));
    }
}
