using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CapturedArrayContainsConstantTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<BasketRow>().Schema.CreateTable();
        db.Table<BasketRow>().Add(new BasketRow { Id = 1, Name = "apple" });
        db.Table<BasketRow>().Add(new BasketRow { Id = 2, Name = "pear" });
        return db;
    }

    [Fact]
    public void CapturedArrayContainsConstantFoldsTrue()
    {
        using TestDatabase db = Seed();
        string[] captured = ["CHERRY", "apple"];

        List<BasketRow> rows = new()
        {
            new BasketRow { Id = 1, Name = "apple" },
            new BasketRow { Id = 2, Name = "pear" },
        };

        List<int> oracle = rows
            .Where(x => x.Id == 1 && captured.Contains("apple"))
            .Select(x => x.Id)
            .ToList();

        List<int> actual = db.Table<BasketRow>()
            .Where(x => x.Id == 1 && captured.Contains("apple"))
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void CapturedArrayContainsConstantFoldsFalse()
    {
        using TestDatabase db = Seed();
        string[] captured = ["CHERRY", "plum"];

        List<BasketRow> rows = new()
        {
            new BasketRow { Id = 1, Name = "apple" },
            new BasketRow { Id = 2, Name = "pear" },
        };

        List<int> oracle = rows
            .Where(x => x.Id == 1 && captured.Contains("apple"))
            .Select(x => x.Id)
            .ToList();

        List<int> actual = db.Table<BasketRow>()
            .Where(x => x.Id == 1 && captured.Contains("apple"))
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(oracle, actual);
    }
}

public class BasketRow
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
