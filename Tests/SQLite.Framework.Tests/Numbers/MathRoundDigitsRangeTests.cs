using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathRoundDigitsRangeTests
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
    public void Round_NegativeDigits_EvaluatesInSqlWhereDotNetThrows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "t", AuthorId = 1, Price = 123.45 });

        List<double> seed = [123.45];
        Assert.Throws<ArgumentOutOfRangeException>(() => seed.Select(p => Math.Round(p, -1)).ToList());

        double actual = db.Table<Book>().Select(b => Math.Round(b.Price, -1)).First();
        Assert.Equal(120.0, actual);
    }

    [Fact]
    public void Round_DigitsAboveFifteen_PreservesValue()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "t", AuthorId = 1, Price = 123.45 });

        List<double> seed = [123.45];
#if NET11_0_OR_GREATER
        Assert.Equal(seed, seed.Select(p => Math.Round(p, 20)).ToList());
#else
        Assert.Throws<ArgumentOutOfRangeException>(() => seed.Select(p => Math.Round(p, 20)).ToList());
#endif

        double actual = db.Table<Book>().Select(b => Math.Round(b.Price, 20)).First();
        Assert.Equal(123.45, actual);
    }

    [Theory]
    [InlineData(1.25, 309)]
    [InlineData(-1.25, int.MaxValue)]
    [InlineData(0.0, 309)]
    [InlineData(1.25e300, 20)]
    [InlineData(1.25e-30, 309)]
    public void Round_ScaleOverflowFallsBackToSqliteRounding(double value, int digits)
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new() { Id = 1, Title = "number", AuthorId = 1, Price = value });
        double expected = db.ExecuteScalar<double>("SELECT ROUND(@value, @digits)",
            new SQLiteParameter { Name = "@value", Value = value },
            new SQLiteParameter { Name = "@digits", Value = digits });
        Assert.Equal(expected, db.Table<Book>().Select(x => Math.Round(x.Price, digits)).Single());
        Assert.Equal(1, db.Table<Book>().Count(x => Math.Round(x.Price, digits) == expected));
    }

    [Fact]
    public void Round_MaterializeFirstUsesExactDotNetRounding()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new() { Id = 1, Title = "number", AuthorId = 1, Price = 2.675 });
        Assert.Equal(2.68, db.Table<Book>().Select(x => Math.Round(x.Price, 2)).Single());
        Assert.Equal(Math.Round(2.675, 2), db.Table<Book>().Select(x => x.Price).ToList().Select(x => Math.Round(x, 2)).Single());
    }

    [Fact]
    public void Round_ComputedOperandsAppearOnceInQuerySql()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new() { Id = 1, Title = "number", AuthorId = -2, Price = -2.125 });

        IQueryable<double> query = db.Table<Book>().Select(x => Math.Round(Math.Abs(x.Price), Math.Abs(x.AuthorId)));
        string sql = query.ToSqlCommand().CommandText;
        Assert.Equal(2, sql.Split("\"BookPrice\"").Length);
        Assert.Equal(2, sql.Split("\"BookAuthorId\"").Length);
        Assert.Equal(2.12, query.Single());
    }

    [Fact]
    public void Round_ComputedColumnUsesSubqueryFreeSql()
    {
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Computed(x => x.Total, x => Math.Round(x.Price * x.Quantity, x.Quantity + 1)));
        db.Schema.CreateTable<ProductLine>();
        db.Execute("INSERT INTO ProductLines (Id, Price, Quantity) VALUES (1, 2.125, 1)");

        Assert.Equal(2.12m, db.Table<ProductLine>().Single().Total);
    }
}
