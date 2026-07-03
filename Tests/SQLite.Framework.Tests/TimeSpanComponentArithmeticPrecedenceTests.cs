using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TimeSpanComponentArithmeticPrecedenceTests
{
    internal sealed class SpanComponentRow
    {
        [Key]
        public int Id { get; set; }

        public TimeSpan Span { get; set; }
    }

    [Fact]
    public void HoursComponentAsRightMultiplyOperandKeepsPrecedence()
    {
        using TestDatabase db = Seed(TimeSpan.FromHours(30));

        int expected = 100 * TimeSpan.FromHours(30).Hours;
        Assert.Equal(600, expected);

        int actual = db.Table<SpanComponentRow>().Select(x => 100 * x.Span.Hours).Single();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HoursComponentAsRightModuloOperandKeepsPrecedence()
    {
        using TestDatabase db = Seed(TimeSpan.FromHours(30));

        int expected = 100 % TimeSpan.FromHours(30).Hours;
        Assert.Equal(4, expected);

        int actual = db.Table<SpanComponentRow>().Select(x => 100 % x.Span.Hours).Single();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Seed(TimeSpan span)
    {
        TestDatabase db = new();
        db.Table<SpanComponentRow>().Schema.CreateTable();
        db.Table<SpanComponentRow>().Add(new SpanComponentRow { Id = 1, Span = span });
        return db;
    }
}
