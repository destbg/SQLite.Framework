using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EqualityComparerOverloadParityTests
{
    private static List<FruitRow> Rows()
    {
        return new List<FruitRow>
        {
            new FruitRow { Id = 1, Name = "apple" },
            new FruitRow { Id = 2, Name = "APPLE" },
            new FruitRow { Id = 3, Name = "Berry" },
            new FruitRow { Id = 4, Name = "berry" },
            new FruitRow { Id = 5, Name = "Cherry" },
        };
    }

    private static List<FruitPair> Pairs()
    {
        return new List<FruitPair>
        {
            new FruitPair { Id = 1, Name = "BERRY" },
            new FruitPair { Id = 2, Name = "date" },
            new FruitPair { Id = 3, Name = "Apple" },
        };
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<FruitRow>().Schema.CreateTable();
        db.Table<FruitPair>().Schema.CreateTable();
        db.Table<FruitRow>().AddRange(Rows());
        db.Table<FruitPair>().AddRange(Pairs());
        return db;
    }

    [Fact]
    public void DistinctWithComparerThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() => db.Table<FruitRow>().Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    [Fact]
    public void TerminalContainsWithComparerThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() => db.Table<FruitRow>().Select(x => x.Name)
            .Contains("CHERRY", StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void IntersectWithComparerThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() => db.Table<FruitRow>().Select(x => x.Name)
            .Intersect(db.Table<FruitPair>().Select(p => p.Name), StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    [Fact]
    public void UnionWithComparerThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() => db.Table<FruitRow>().Select(x => x.Name)
            .Union(db.Table<FruitPair>().Select(p => p.Name), StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    [Fact]
    public void ExceptWithComparerThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() => db.Table<FruitRow>().Select(x => x.Name)
            .Except(db.Table<FruitPair>().Select(p => p.Name), StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    [Fact]
    public void CollectionContainsInWhereWithComparerThrows()
    {
        using TestDatabase db = Seed();
        string[] wanted = ["BERRY", "date"];

        Assert.Throws<NotSupportedException>(() => db.Table<FruitRow>()
            .Where(x => wanted.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
            .Select(x => x.Id)
            .ToList());
    }

    [Fact]
    public void GroupByWithComparerThrows()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() => db.Table<FruitRow>()
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Count());
    }
}

public class FruitRow
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class FruitPair
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
