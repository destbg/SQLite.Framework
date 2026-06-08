using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathRoundDigitsRangeParityTests
{
    [Fact]
    public void Round_DecimalColumn_DigitsUpToTwentyEight_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, DecimalValue = 10m });

        decimal expected = Math.Round(10m, 20, MidpointRounding.AwayFromZero);
        decimal actual = db.Table<NumericType>().Select(x => Math.Round(x.DecimalValue, 20, MidpointRounding.AwayFromZero)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Round_NegativeDigits_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "t", AuthorId = 1, Price = 123.45 });

        List<double> seed = [123.45];
        Assert.Throws<ArgumentOutOfRangeException>(() => seed.Select(p => Math.Round(p, -1)).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Table<Book>().Select(b => Math.Round(b.Price, -1)).ToList());
    }

    [Fact]
    public void Round_DigitsAboveFifteen_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "t", AuthorId = 1, Price = 123.45 });

        List<double> seed = [123.45];
        Assert.Throws<ArgumentOutOfRangeException>(() => seed.Select(p => Math.Round(p, 20)).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Table<Book>().Select(b => Math.Round(b.Price, 20)).ToList());
    }
}
