using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathOperationTests
{
    [Fact]
    public void MathAbs()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Abs(book.Price - 10) < 1
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(10d, command.Parameters[0].Value);
        Assert.Equal(1d, command.Parameters[1].Value);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE ABS((b0.\"BookPrice\" - @p0)) < @p1", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathRound()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Round(book.Price) == 10
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(10d, command.Parameters[0].Value);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE (CASE WHEN ABS(b0.\"BookPrice\" - ROUND(b0.\"BookPrice\")) = 0.5 THEN 2 * ROUND(b0.\"BookPrice\" / 2) ELSE ROUND(b0.\"BookPrice\") END) = @p0", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathRoundWithDigits()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Round(book.Price, 2) == 10.50
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(2, command.Parameters[0].Value);
        Assert.Equal(10.50d, command.Parameters[1].Value);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE ((CASE WHEN ABS((b0.\"BookPrice\" * POWER(10, @p0)) - ROUND((b0.\"BookPrice\" * POWER(10, @p0)))) = 0.5 THEN 2 * ROUND((b0.\"BookPrice\" * POWER(10, @p0)) / 2) ELSE ROUND((b0.\"BookPrice\" * POWER(10, @p0))) END) / POWER(10, @p0)) = @p1", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathPow()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Pow(book.Id, 2) == 4
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(2d, command.Parameters[0].Value);
        Assert.Equal(4d, command.Parameters[1].Value);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE POWER(CAST(b0.\"BookId\" AS REAL), @p0) = @p1", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathFloor()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Floor(book.Price) == 10
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(10d, command.Parameters[0].Value);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE FLOOR(b0.\"BookPrice\") = @p0", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathCeiling()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where Math.Ceiling(book.Price) == 11
            select book
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(11d, command.Parameters[0].Value);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE CEIL(b0.\"BookPrice\") = @p0", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathMaxWithResults()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 10 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 2, Price = 15 }
        });

        var query = db.Table<Book>()
            .Select(b => new { b.Id, MaxPrice = Math.Max(b.Price, 8) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(8d, command.Parameters[0].Value);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       MAX(b0.\"BookPrice\", @p0) AS \"MaxPrice\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));

        var result = query.ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(8, result[0].MaxPrice);
        Assert.Equal(10, result[1].MaxPrice);
        Assert.Equal(15, result[2].MaxPrice);
    }

    [Fact]
    public void MathMinWithResults()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 5 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 10 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 2, Price = 15 }
        });

        var query = db.Table<Book>()
            .Select(b => new { b.Id, MinPrice = Math.Min(b.Price, 8) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(8d, command.Parameters[0].Value);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       MIN(b0.\"BookPrice\", @p0) AS \"MinPrice\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));

        var result = query.ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(5, result[0].MinPrice);
        Assert.Equal(8, result[1].MinPrice);
        Assert.Equal(8, result[2].MinPrice);
    }

    [Fact]
    public void ArithmeticAddition()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            select book.Price + 5
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(5d, command.Parameters[0].Value);
        Assert.Equal("SELECT (b0.\"BookPrice\" + @p0) AS \"6\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ArithmeticSubtraction()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            select book.Price - 5
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(5d, command.Parameters[0].Value);
        Assert.Equal("SELECT (b0.\"BookPrice\" - @p0) AS \"6\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ArithmeticMultiplication()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            select book.Price * 1.1
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(1.1d, command.Parameters[0].Value);
        Assert.Equal("SELECT (b0.\"BookPrice\" * @p0) AS \"6\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ArithmeticDivision()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            select book.Price / 2
        ).ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(2d, command.Parameters[0].Value);
        Assert.Equal("SELECT (b0.\"BookPrice\" / @p0) AS \"6\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ArithmeticModulo()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where book.Id % 2 == 0
            select book
        ).ToSqlCommand();

        Assert.Equal(2, command.Parameters.Count);
        Assert.Equal(2, command.Parameters[0].Value);
        Assert.Equal(0, command.Parameters[1].Value);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE (b0.\"BookId\" % @p0) = @p1", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ComplexMathExpression()
    {
        using TestDatabase db = new();

        SQLiteCommand command = (
            from book in db.Table<Book>()
            where (book.Price * 1.1) + 5 > 20
            select book
        ).ToSqlCommand();

        Assert.Equal(3, command.Parameters.Count);
        Assert.Equal(1.1d, command.Parameters[0].Value);
        Assert.Equal(5d, command.Parameters[1].Value);
        Assert.Equal(20d, command.Parameters[2].Value);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       b0.\"BookTitle\" AS \"Title\",\n       b0.\"BookAuthorId\" AS \"AuthorId\",\n       b0.\"BookPrice\" AS \"Price\"\nFROM \"Books\" AS b0\nWHERE ((b0.\"BookPrice\" * @p0) + @p1) > @p2", command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MathSign()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = -5 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 0 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 2, Price = 10 }
        });

        var query = db.Table<Book>().Select(b => new { b.Id, Sign = Math.Sign(b.Price) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       (CASE WHEN b0.\"BookPrice\" > 0 THEN 1 WHEN b0.\"BookPrice\" < 0 THEN -1 ELSE 0 END) AS \"Sign\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(3, results.Count);
        Assert.Equal(-1, results[0].Sign);
        Assert.Equal(0, results[1].Sign);
        Assert.Equal(1, results[2].Sign);
    }

    [Fact]
    public void MathSqrt()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 4 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 9 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 2, Price = 16 }
        });

        var query = db.Table<Book>().Select(b => new { b.Id, Sqrt = Math.Sqrt(b.Price) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       SQRT(b0.\"BookPrice\") AS \"Sqrt\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(3, results.Count);
        Assert.Equal(2, results[0].Sqrt);
        Assert.Equal(3, results[1].Sqrt);
        Assert.Equal(4, results[2].Sqrt);
    }

    [Fact]
    public void MathExp()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 0 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 1 }
        });

        var query = db.Table<Book>().Select(b => new { b.Id, Exp = Math.Exp(b.Price) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       EXP(b0.\"BookPrice\") AS \"Exp\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].Exp, 5);
        Assert.Equal(Math.E, results[1].Exp, 5);
    }

    [Fact]
    public void MathLog()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 10 }
        });

        var query = db.Table<Book>().Select(b => new { b.Id, Log = Math.Log(b.Price) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       LN(b0.\"BookPrice\") AS \"Log\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal(Math.Log(1), results[0].Log, 5);
        Assert.Equal(Math.Log(10), results[1].Log, 5);
    }

    [Fact]
    public void MathLogWithBase()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 8 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 16 }
        });

        var query = db.Table<Book>().Select(b => new { b.Id, Log = Math.Log(b.Price, 2) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(2d, command.Parameters[0].Value);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       LOG(@p0, b0.\"BookPrice\") AS \"Log\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal(3, results[0].Log, 5);
        Assert.Equal(4, results[1].Log, 5);
    }

    [Fact]
    public void MathLog10()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 100 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 2, Price = 1000 }
        });

        var query = db.Table<Book>().Select(b => new { b.Id, Log10 = Math.Log10(b.Price) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       LOG10(b0.\"BookPrice\") AS \"Log10\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].Log10, 5);
        Assert.Equal(2, results[1].Log10, 5);
        Assert.Equal(3, results[2].Log10, 5);
    }

    [Fact]
    public void DoubleDegreesToRadians()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 0 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = 90 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 2, Price = 180 }
        });

        var query = db.Table<Book>().Select(b => new { b.Id, Radians = double.DegreesToRadians(b.Price) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       RADIANS(b0.\"BookPrice\") AS \"Radians\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(3, results.Count);
        Assert.Equal(0, results[0].Radians, 5);
        Assert.Equal(Math.PI / 2, results[1].Radians, 5);
        Assert.Equal(Math.PI, results[2].Radians, 5);
    }

    [Fact]
    public void DoubleRadiansToDegrees()
    {
        using TestDatabase db = new();

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 0 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 1, Price = Math.PI / 2 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 2, Price = Math.PI }
        });

        var query = db.Table<Book>().Select(b => new { b.Id, Degrees = double.RadiansToDegrees(b.Price) });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Empty(command.Parameters);
        Assert.Equal("SELECT b0.\"BookId\" AS \"Id\",\n       DEGREES(b0.\"BookPrice\") AS \"Degrees\"\nFROM \"Books\" AS b0", command.CommandText.Replace("\r\n", "\n"));

        var results = query.ToList();
        Assert.Equal(3, results.Count);
        Assert.Equal(0, results[0].Degrees, 5);
        Assert.Equal(90, results[1].Degrees, 5);
        Assert.Equal(180, results[2].Degrees, 5);
    }

    [Fact]
    public void MathPiIsInlinedAsConstant()
    {
        using TestDatabase db = new();

        var query = db.Table<Book>().Select(b => new { b.Id, TwoPi = b.Price * Math.PI });

        SQLiteCommand command = query.ToSqlCommand();

        Assert.Single(command.Parameters);
        Assert.Equal(Math.PI, command.Parameters[0].Value);
    }
}
