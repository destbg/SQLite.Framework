using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathRoundMidpointTests
{
    private static TestDatabase Prices(params double[] prices)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        int id = 1;
        foreach (double p in prices)
        {
            db.Table<Book>().Add(new Book { Id = id, Title = "t" + id, AuthorId = 1, Price = p });
            id++;
        }

        return db;
    }

    [Fact]
    public void MathRoundSingleArgUsesBankersRoundingLikeDotNet()
    {
        using TestDatabase db = Prices(0.5, 2.5, 4.5, 3.5);

        List<double> actual = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => Math.Round(b.Price))
            .ToList();

        List<double> oracle = new[] { 0.5, 2.5, 4.5, 3.5 }
            .Select(x => Math.Round(x))
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void MathRoundWithDigitsUsesBankersRoundingLikeDotNet()
    {
        using TestDatabase db = Prices(2.125, 2.135);

        List<double> actual = db.Table<Book>()
            .OrderBy(b => b.Id)
            .Select(b => Math.Round(b.Price, 2))
            .ToList();

        List<double> oracle = new[] { 2.125, 2.135 }
            .Select(x => Math.Round(x, 2))
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
