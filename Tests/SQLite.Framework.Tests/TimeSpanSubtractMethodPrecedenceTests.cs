using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class SpanSubtractRow
{
    [Key]
    public int Id { get; set; }

    public TimeSpan Span { get; set; }
}

public class TimeSpanSubtractMethodPrecedenceTests
{
    [Fact]
    public void SubtractMethodAsRightOperandKeepsPrecedence()
    {
        using TestDatabase db = new();
        db.Table<SpanSubtractRow>().Schema.CreateTable();
        db.Table<SpanSubtractRow>().Add(new SpanSubtractRow { Id = 1, Span = TimeSpan.FromHours(10) });

        TimeSpan c = TimeSpan.FromHours(2);
        TimeSpan expected = TimeSpan.FromHours(10) - TimeSpan.FromHours(10).Subtract(c);
        Assert.Equal(TimeSpan.FromHours(2), expected);

        TimeSpan actual = db.Table<SpanSubtractRow>().Select(x => x.Span - x.Span.Subtract(c)).Single();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SubtractMethodBesideAddKeepsPrecedence()
    {
        using TestDatabase db = new();
        db.Table<SpanSubtractRow>().Schema.CreateTable();
        db.Table<SpanSubtractRow>().Add(new SpanSubtractRow { Id = 1, Span = TimeSpan.FromHours(10) });

        TimeSpan c = TimeSpan.FromHours(2);
        TimeSpan expected = TimeSpan.FromHours(10).Add(c) - TimeSpan.FromHours(10).Subtract(c);
        Assert.Equal(TimeSpan.FromHours(4), expected);

        TimeSpan actual = db.Table<SpanSubtractRow>().Select(x => x.Span.Add(c) - x.Span.Subtract(c)).Single();

        Assert.Equal(expected, actual);
    }
}
