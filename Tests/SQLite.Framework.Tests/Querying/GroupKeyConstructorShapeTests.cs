using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GksRows")]
public class GksRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class GksScaledKey
{
    public GksScaledKey(int value)
    {
        Value = value * 10;
    }

    public int Value { get; set; }
}

public class GroupKeyConstructorShapeTests
{
    [Fact]
    public void ATupleCreateKeyGroupsLikeLinqToObjects()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows()
            .GroupBy(r => Tuple.Create(r.A, r.B))
            .Select(g => g.Count())
            .OrderBy(c => c)
            .ToList();

        List<int> actual = db.Table<GksRow>()
            .GroupBy(r => Tuple.Create(r.A, r.B))
            .Select(g => g.Count())
            .OrderBy(c => c)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ATransformingConstructorKeyGroupsByItsArgument()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows()
            .GroupBy(r => r.A)
            .Select(g => g.Count())
            .OrderBy(c => c)
            .ToList();

        List<int> actual = db.Table<GksRow>()
            .GroupBy(r => new GksScaledKey(r.A))
            .Select(g => g.Count())
            .OrderBy(c => c)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AGroupingContainsWithAComparerReportsItCannotRun()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() => db.Table<GksRow>()
            .GroupBy(r => r.A)
            .Select(g => g.Select(x => x.B).Contains(5, EqualityComparer<int>.Default))
            .ToList());
    }

    [Fact]
    public void AGroupingAppendWithAValueReportsItCannotRun()
    {
        using TestDatabase db = Seed();

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<GksRow>()
            .GroupBy(r => r.A)
            .Select(g => g.Append(new GksRow()).Count())
            .ToList());

        Assert.Contains("Append", exception.Message);
    }

    private static List<GksRow> Rows()
    {
        return
        [
            new GksRow { Id = 1, A = 1, B = 1 },
            new GksRow { Id = 2, A = 1, B = 1 },
            new GksRow { Id = 3, A = 2, B = 3 }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<GksRow>().Schema.CreateTable();
        db.Table<GksRow>().AddRange(Rows());
        return db;
    }
}
