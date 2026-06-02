using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class MathRoundAwayFromZeroBugTests
{
    [Fact]
    public void RoundWithDigits_AwayFromZero_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "t", AuthorId = 1, Price = 2.675 });

        double expected = new[] { 2.675 }.Select(x => Math.Round(x, 2, MidpointRounding.AwayFromZero)).First();
        double actual = db.Table<Book>().Select(b => Math.Round(b.Price, 2, MidpointRounding.AwayFromZero)).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RoundNoDigits_AwayFromZero_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "t", AuthorId = 1, Price = 0.49999999999999994 });

        double expected = new[] { 0.49999999999999994 }.Select(x => Math.Round(x, MidpointRounding.AwayFromZero)).First();
        double actual = db.Table<Book>().Select(b => Math.Round(b.Price, MidpointRounding.AwayFromZero)).First();

        Assert.Equal(expected, actual);
    }
}
