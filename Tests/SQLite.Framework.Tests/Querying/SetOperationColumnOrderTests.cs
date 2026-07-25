using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class SetOpOrderedPair
{
    public int A { get; set; }
    public int B { get; set; }
}

file sealed class TwoIntRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public int A { get; set; }
    public int B { get; set; }
}

public class SetOperationColumnOrderTests
{
    [Fact]
    public void RecursiveCte_SwappedBindingOrder_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        SQLiteCte<SetOpOrderedPair> cte = db.WithRecursive<SetOpOrderedPair>(self =>
            db.Values(new SetOpOrderedPair { A = 0, B = 100 })
                .Concat(from p in self
                    where p.A < 3
                    select new SetOpOrderedPair { B = p.B + 1, A = p.A + 1 }));

        List<(int A, int B)> actual = (from p in cte select p)
            .ToList()
            .Select(p => (p.A, p.B))
            .OrderBy(t => t.A)
            .ToList();

        Assert.Equal([(0, 100), (1, 101), (2, 102), (3, 103)], actual);
    }

    [Fact]
    public void Concat_SwappedBindingOrder_DoesNotSwapColumns()
    {
        using TestDatabase db = new();
        db.Table<TwoIntRow>().Schema.CreateTable();
        db.Table<TwoIntRow>().Add(new TwoIntRow { Id = 1, A = 1, B = 10 });
        db.Table<TwoIntRow>().Add(new TwoIntRow { Id = 2, A = 2, B = 20 });

        List<(int A, int B)> actual = db.Table<TwoIntRow>().Where(r => r.Id == 1)
            .Select(r => new SetOpOrderedPair { A = r.A, B = r.B })
            .Concat(db.Table<TwoIntRow>().Where(r => r.Id == 2)
                .Select(r => new SetOpOrderedPair { B = r.B, A = r.A }))
            .ToList()
            .Select(p => (p.A, p.B))
            .OrderBy(t => t.A)
            .ToList();

        Assert.Equal([(1, 10), (2, 20)], actual);
    }

    [Fact]
    public void Union_SwappedBindingOrder_DoesNotSwapColumns()
    {
        using TestDatabase db = new();
        db.Table<TwoIntRow>().Schema.CreateTable();
        db.Table<TwoIntRow>().Add(new TwoIntRow { Id = 1, A = 5, B = 50 });

        List<(int A, int B)> actual = db.Table<TwoIntRow>()
            .Select(r => new SetOpOrderedPair { A = r.A, B = r.B })
            .Union(db.Table<TwoIntRow>().Select(r => new SetOpOrderedPair { B = r.B, A = r.A }))
            .ToList()
            .Select(p => (p.A, p.B))
            .ToList();

        Assert.Equal([(5, 50)], actual);
    }

    [Fact]
    public void NormalSelect_SwappedBindingOrder_MaterializesCorrectly()
    {
        using TestDatabase db = new();
        db.Table<TwoIntRow>().Schema.CreateTable();
        db.Table<TwoIntRow>().Add(new TwoIntRow { Id = 1, A = 7, B = 70 });

        SetOpOrderedPair pair = db.Table<TwoIntRow>().Select(r => new SetOpOrderedPair { B = r.B, A = r.A }).First();

        Assert.Equal(7, pair.A);
        Assert.Equal(70, pair.B);
    }
}
