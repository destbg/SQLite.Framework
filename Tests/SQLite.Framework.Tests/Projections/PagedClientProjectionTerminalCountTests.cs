using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24aPagedCountRows")]
public class H24aPagedCountRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H24aPagedCountText
{
    public static string Tail(string value)
    {
        return value.Substring(value.Length - 1);
    }
}

public class PagedClientProjectionTerminalCountTests
{
    [Fact]
    public void CountOverAClientProjectionOfATakenSourceCountsTheTakenRows()
    {
        using TestDatabase db = Setup(nameof(CountOverAClientProjectionOfATakenSourceCountsTheTakenRows));

        int expected = Rows()
            .Take(2)
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Count();

        int actual = db.Table<H24aPagedCountRow>()
            .Take(2)
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountOverAClientProjectionOfASkippedSourceCountsTheRemainingRows()
    {
        using TestDatabase db = Setup(nameof(CountOverAClientProjectionOfASkippedSourceCountsTheRemainingRows));

        int expected = Rows()
            .Skip(2)
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Count();

        int actual = db.Table<H24aPagedCountRow>()
            .Skip(2)
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountAfterTakeOnAClientProjectionCountsTheTakenValues()
    {
        using TestDatabase db = Setup(nameof(CountAfterTakeOnAClientProjectionCountsTheTakenValues));

        int expected = Rows()
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Take(2)
            .Count();

        int actual = db.Table<H24aPagedCountRow>()
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Take(2)
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongCountOverAClientProjectionOfATakenSourceCountsTheTakenRows()
    {
        using TestDatabase db = Setup(nameof(LongCountOverAClientProjectionOfATakenSourceCountsTheTakenRows));

        long expected = Rows()
            .Take(2)
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .LongCount();

        long actual = db.Table<H24aPagedCountRow>()
            .Take(2)
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .LongCount();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountAfterSkipOnAClientProjectionCountsTheRemainingValues()
    {
        using TestDatabase db = Setup(nameof(CountAfterSkipOnAClientProjectionCountsTheRemainingValues));

        int expected = Rows()
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Skip(2)
            .Count();

        int actual = db.Table<H24aPagedCountRow>()
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Skip(2)
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountAfterSkipAndTakeOnAClientProjectionCountsTheWindow()
    {
        using TestDatabase db = Setup(nameof(CountAfterSkipAndTakeOnAClientProjectionCountsTheWindow));

        int expected = Rows()
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Skip(1)
            .Take(2)
            .Count();

        int actual = db.Table<H24aPagedCountRow>()
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Skip(1)
            .Take(2)
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongCountAfterSkipOnAClientProjectionCountsTheRemainingValues()
    {
        using TestDatabase db = Setup(nameof(LongCountAfterSkipOnAClientProjectionCountsTheRemainingValues));

        long expected = Rows()
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Skip(2)
            .LongCount();

        long actual = db.Table<H24aPagedCountRow>()
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Skip(2)
            .LongCount();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountAfterASkipOverASqlPagedClientProjectionCountsTheRemainingValues()
    {
        using TestDatabase db = Setup(nameof(CountAfterASkipOverASqlPagedClientProjectionCountsTheRemainingValues));

        int expected = Rows()
            .OrderBy(r => r.Id)
            .Take(4)
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Skip(2)
            .Count();

        int actual = db.Table<H24aPagedCountRow>()
            .OrderBy(r => r.Id)
            .Take(4)
            .Select(r => H24aPagedCountText.Tail(r.Name))
            .Skip(2)
            .Count();

        Assert.Equal(expected, actual);
    }

    private static List<H24aPagedCountRow> Rows()
    {
        return
        [
            new H24aPagedCountRow { Id = 1, Name = "1a" },
            new H24aPagedCountRow { Id = 2, Name = "2b" },
            new H24aPagedCountRow { Id = 3, Name = "3c" },
            new H24aPagedCountRow { Id = 4, Name = "4d" },
            new H24aPagedCountRow { Id = 5, Name = "5e" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24aPagedCountRow>().Schema.CreateTable();
        db.Table<H24aPagedCountRow>().AddRange(Rows());
        return db;
    }
}
