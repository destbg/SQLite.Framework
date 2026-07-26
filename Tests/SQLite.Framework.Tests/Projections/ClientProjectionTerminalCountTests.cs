using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22hClientCountRows")]
public class H22hClientCountRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H22hCountText
{
    public static string Head(string value)
    {
        return value.Substring(0, 1);
    }
}

public class ClientProjectionTerminalCountTests
{
    [Fact]
    public void CountOverAClientProjectionCountsTheRows()
    {
        using TestDatabase db = Setup(nameof(CountOverAClientProjectionCountsTheRows));
        List<H22hClientCountRow> local = Rows();

        int expected = local
            .Select(r => H22hCountText.Head(r.Name))
            .Count();

        int actual = db.Table<H22hClientCountRow>()
            .Select(r => H22hCountText.Head(r.Name))
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountAfterDistinctOverAClientProjectionCountsProjectedValues()
    {
        using TestDatabase db = Setup(nameof(CountAfterDistinctOverAClientProjectionCountsProjectedValues));
        List<H22hClientCountRow> local = Rows();

        int expected = local
            .Select(r => H22hCountText.Head(r.Name))
            .Distinct()
            .Count();

        int actual = db.Table<H22hClientCountRow>()
            .Select(r => H22hCountText.Head(r.Name))
            .Distinct()
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongCountAfterDistinctOverAClientProjectionCountsProjectedValues()
    {
        using TestDatabase db = Setup(nameof(LongCountAfterDistinctOverAClientProjectionCountsProjectedValues));
        List<H22hClientCountRow> local = Rows();

        long expected = local
            .Select(r => H22hCountText.Head(r.Name))
            .Distinct()
            .LongCount();

        long actual = db.Table<H22hClientCountRow>()
            .Select(r => H22hCountText.Head(r.Name))
            .Distinct()
            .LongCount();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnyOverAClientProjectionSeesTheRows()
    {
        using TestDatabase db = Setup(nameof(AnyOverAClientProjectionSeesTheRows));
        List<H22hClientCountRow> local = Rows();

        bool expected = local
            .Select(r => H22hCountText.Head(r.Name))
            .Distinct()
            .Any();

        bool actual = db.Table<H22hClientCountRow>()
            .Select(r => H22hCountText.Head(r.Name))
            .Distinct()
            .Any();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SumOverAClientProjectionExplainsTheLimit()
    {
        using TestDatabase db = Setup(nameof(SumOverAClientProjectionExplainsTheLimit));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(
            () => db.Table<H22hClientCountRow>().Select(r => H22hCountText.Head(r.Name).Length).Sum());
        Assert.Contains("Materialize the values with ToList", exception.Message);
    }

    [Fact]
    public void StringJoinOverAClientProjectionExplainsTheLimit()
    {
        using TestDatabase db = Setup(nameof(StringJoinOverAClientProjectionExplainsTheLimit));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(
            () => db.Table<H22hClientCountRow>().Select(r => H22hCountText.Head(r.Name)).StringJoin(", "));
        Assert.Contains("call string.Join in memory", exception.Message);
    }

    private static List<H22hClientCountRow> Rows()
    {
        return
        [
            new H22hClientCountRow { Id = 1, Name = "ax" },
            new H22hClientCountRow { Id = 2, Name = "ay" },
            new H22hClientCountRow { Id = 3, Name = "bz" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H22hClientCountRow>().Schema.CreateTable();
        db.Table<H22hClientCountRow>().AddRange(Rows());
        return db;
    }
}
