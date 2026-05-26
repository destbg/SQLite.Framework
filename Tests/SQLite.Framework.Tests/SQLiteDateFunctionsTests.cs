using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SQLiteDateFunctionsTests
{
    [Fact]
    public void Date_NoArgs_ReturnsCurrentDate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string today = db.Table<Book>().Select(b => SQLiteDateFunctions.Date()).First();

        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", today);
    }

    [Fact]
    public void Date_WithLiteral_ParsesAndReturnsDate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>().Select(b => SQLiteDateFunctions.Date("2024-06-15")).First();

        Assert.Equal("2024-06-15", result);
    }

    [Fact]
    public void Date_WithModifier_AddsDays()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>().Select(b => SQLiteDateFunctions.Date("2024-06-15", "+7 days")).First();

        Assert.Equal("2024-06-22", result);
    }

    [Fact]
    public void Time_WithLiteral_ReturnsTime()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>().Select(b => SQLiteDateFunctions.Time("2024-06-15 12:30:45")).First();

        Assert.Equal("12:30:45", result);
    }

    [Fact]
    public void Time_NoArgs_EmitsTimeCall()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.Table<Book>().Select(b => SQLiteDateFunctions.Time()).ToSqlCommand();

        Assert.Equal("""
                     SELECT time() AS "5"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            cmd.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Datetime_WithLiteral_ReturnsCombined()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>().Select(b => SQLiteDateFunctions.Datetime("2024-06-15 12:30:45")).First();

        Assert.Equal("2024-06-15 12:30:45", result);
    }

    [Fact]
    public void Datetime_NoArgs_EmitsDatetimeCall()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.Table<Book>().Select(b => SQLiteDateFunctions.Datetime()).ToSqlCommand();

        Assert.Equal("""
                     SELECT datetime() AS "5"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            cmd.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void JulianDay_WithLiteral_ReturnsNumber()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        double jd = db.Table<Book>().Select(b => SQLiteDateFunctions.JulianDay("2024-06-15")).First();

        Assert.True(jd > 2460000);
    }

    [Fact]
    public void JulianDay_NoArgs_EmitsJulianDayCall()
    {
        using TestDatabase db = new();

        SQLiteCommand cmd = db.Table<Book>().Select(b => SQLiteDateFunctions.JulianDay()).ToSqlCommand();

        Assert.Equal("""
                     SELECT julianday() AS "5"
                     FROM "Books" AS b0
                     """.Replace("\r\n", "\n"),
            cmd.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Strftime_FormatsYear()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string year = db.Table<Book>().Select(b => SQLiteDateFunctions.Strftime("%Y", "2024-06-15")).First();

        Assert.Equal("2024", year);
    }

    [Fact]
    public void Strftime_WithModifier_AppliesIt()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string month = db.Table<Book>().Select(b => SQLiteDateFunctions.Strftime("%m", "2024-06-15", "+1 month")).First();

        Assert.Equal("07", month);
    }

    [Fact]
    public void Date_WithCapturedModifierArray_AppliesModifiers()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string[] modifiers = ["+1 month", "+1 day"];
        string result = db.Table<Book>()
            .Select(b => SQLiteDateFunctions.Date("2024-06-15", modifiers))
            .First();

        Assert.Equal("2024-07-16", result);
    }

    [Fact]
    public void Date_NumericJulianDay_ReturnsDate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>()
            .Select(b => SQLiteDateFunctions.Date(2460476.5))
            .First();

        Assert.Equal("2024-06-15", result);
    }

    [Fact]
    public void Datetime_NumericUnixEpoch_WithModifier_ReturnsDatetime()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>()
            .Select(b => SQLiteDateFunctions.Datetime(1718409600L, "unixepoch"))
            .First();

        Assert.Equal("2024-06-15 00:00:00", result);
    }

    [Fact]
    public void Strftime_NumericInput_FormatsCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string year = db.Table<Book>()
            .Select(b => SQLiteDateFunctions.Strftime("%Y", 1718409600, "unixepoch"))
            .First();

        Assert.Equal("2024", year);
    }

    [Fact]
    public void JulianDay_NumericInput_PassesThrough()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        double jd = db.Table<Book>()
            .Select(b => SQLiteDateFunctions.JulianDay(2460476.5))
            .First();

        Assert.Equal(2460476.5, jd);
    }

    [Fact]
    public void Time_NumericJulianDay_ReturnsTime()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string result = db.Table<Book>()
            .Select(b => SQLiteDateFunctions.Time(2460476.5))
            .First();

        Assert.Equal("00:00:00", result);
    }

#if !SQLITECIPHER
    [Fact]
    public void Timediff_ReturnsDifferenceString()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        string diff = db.Table<Book>()
            .Select(b => SQLiteDateFunctions.Timediff("2024-06-22", "2024-06-15"))
            .First();

        Assert.Contains("+0000-00-07", diff);
    }
#endif

    [Fact]
    public void Date_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Date());
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Date("now"));
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Date(2460476.5));
    }

    [Fact]
    public void Time_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Time());
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Time("now"));
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Time(2460476.5));
    }

    [Fact]
    public void Datetime_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Datetime());
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Datetime("now"));
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Datetime(2460476.5));
    }

    [Fact]
    public void JulianDay_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.JulianDay());
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.JulianDay("now"));
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.JulianDay(2460476.5));
    }

    [Fact]
    public void Strftime_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Strftime("%Y", "now"));
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Strftime("%Y", 2460476.5));
    }

#if !SQLITECIPHER
    [Fact]
    public void Timediff_OutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Timediff("a", "b"));
        Assert.Throws<InvalidOperationException>(() => SQLiteDateFunctions.Timediff(2460476.5, 2460475.5));
    }
#endif
}
