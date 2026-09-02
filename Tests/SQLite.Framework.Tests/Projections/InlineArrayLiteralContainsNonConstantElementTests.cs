using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLitePCL;

namespace SQLite.Framework.Tests;

public class InlineArrayLiteralContainsNonConstantElementTests
{
    internal sealed class IalRow
    {
        [Key]
        public int Id { get; set; }

        public int V { get; set; }
    }

    private static readonly IalRow[] Data =
    [
        new IalRow { Id = 1, V = 10 },
        new IalRow { Id = 2, V = 20 },
        new IalRow { Id = 3, V = 30 },
    ];

    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<IalRow>().Schema.CreateTable();
        foreach (IalRow r in Data)
        {
            db.Table<IalRow>().Add(r);
        }

        return db;
    }

    [Fact]
    public void ContainsOverArrayWithComputedElementsMatchesLinq()
    {
        using TestDatabase db = Create();

        List<int> expected = Data
            .Where(x => new[] { int.Parse("10"), int.Parse("30") }.Contains(x.V))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> actual = db.Table<IalRow>()
            .Where(x => new[] { int.Parse("10"), int.Parse("30") }.Contains(x.V))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsOverArrayWithConstantAndComputedElementsMatchesLinq()
    {
        using TestDatabase db = Create();

        List<int> expected = Data
            .Where(x => new[] { 10, int.Parse("30") }.Contains(x.V))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> actual = db.Table<IalRow>()
            .Where(x => new[] { 10, int.Parse("30") }.Contains(x.V))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsOverArrayWithConstantInstanceMethodMatchesLinq()
    {
        using TestDatabase db = Create();

        List<int> expected = Data
            .Where(x => new[] { "10".IndexOf("0", StringComparison.Ordinal), int.Parse("30") }.Contains(x.V))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> actual = db.Table<IalRow>()
            .Where(x => new[] { "10".IndexOf("0", StringComparison.Ordinal), int.Parse("30") }.Contains(x.V))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsOverArrayWithRowElementMatchesLinq()
    {
        using TestDatabase db = Create();

        List<int> expected = Data
            .Where(x => new[] { x.V, int.Parse("30") }.Contains(x.V))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> actual = db.Table<IalRow>()
            .Where(x => new[] { x.V, int.Parse("30") }.Contains(x.V))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([1, 2, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsOverArrayWithUserRowMethodStaysUnsupported()
    {
        using TestDatabase db = Create();

        Assert.Throws<NotSupportedException>(() => db.Table<IalRow>()
            .Where(x => new[] { Identity(x.V), int.Parse("30") }.Contains(x.V))
            .ToList());
    }

    [Fact]
    public void UserRowMethodInsideOtherInlineArrayStaysUnsupported()
    {
        using TestDatabase db = Create();

        Assert.Throws<NotSupportedException>(() => db.Table<IalRow>()
            .Where(x => new[] { Identity(x.V) }.Sum() > 0)
            .ToList());
    }

    [Fact]
    public void ContainsOverArrayWithTranslatableRowMethodMatchesLinq()
    {
        using TestDatabase db = Create();

        List<int> expected = Data
            .Where(x => new[] { int.Parse(x.V.ToString()), int.Parse("30") }.Contains(x.V))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> actual = db.Table<IalRow>()
            .Where(x => new[] { int.Parse(x.V.ToString()), int.Parse("30") }.Contains(x.V))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([1, 2, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsOverCapturedArrayWithComputedElementsWorks()
    {
        using TestDatabase db = Create();
        int[] values = [int.Parse("10"), int.Parse("30")];

        List<int> expected = Data.Where(x => values.Contains(x.V)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<IalRow>().Where(x => values.Contains(x.V)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilteredContainsOverLiteralConstantArrayMatchesLinq()
    {
        using TestDatabase db = Create();

        List<int> expected = Data
            .Where(x => new[] { 10, 20, 30 }.Where(v => v >= 20).Contains(x.V))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();
        List<int> actual = db.Table<IalRow>()
            .Where(x => new[] { 10, 20, 30 }.Where(v => v >= 20).Contains(x.V))
            .Select(x => x.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal([2, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LargeContainsCollectionUsesTheConnectionParameterLimit()
    {
        using TestDatabase db = Create();
        int[] values = Enumerable.Range(0, 1100).ToArray();
        int minimumId = 0;
        db.OpenConnection();
        int oldLimit = raw.sqlite3_limit(db.Handle!, raw.SQLITE_LIMIT_VARIABLE_NUMBER, 64);

        try
        {
            List<int> expected = Data
                .Where(x => values.Contains(x.V) && x.Id > minimumId)
                .Select(x => x.Id)
                .OrderBy(i => i)
                .ToList();
            IQueryable<IalRow> query = db.Table<IalRow>()
                .Where(x => values.Contains(x.V) && x.Id > minimumId);
            SQLiteCommand command = query.ToSqlCommand();
            List<int> actual = query.Select(x => x.Id).OrderBy(i => i).ToList();

            Assert.Equal([1, 2, 3], expected);
            Assert.Equal(expected, actual);
            Assert.Equal(64, command.Parameters.Count);
        }
        finally
        {
            raw.sqlite3_limit(db.Handle!, raw.SQLITE_LIMIT_VARIABLE_NUMBER, oldLimit);
        }
    }

    [Fact]
    public void QueryAboveTheConnectionParameterLimitHasAUsefulMessage()
    {
        using TestDatabase db = Create();
        int[] values = [10];
        int a = 101;
        int b = 102;
        int c = 103;
        int d = 104;
        db.OpenConnection();
        int oldLimit = raw.sqlite3_limit(db.Handle!, raw.SQLITE_LIMIT_VARIABLE_NUMBER, 3);

        try
        {
            IQueryable<IalRow> query = db.Table<IalRow>()
                .Where(x => values.Contains(x.V)
                    && x.Id != a
                    && x.Id != b
                    && x.Id != c
                    && x.Id != d);

            NotSupportedException exception = Assert.Throws<NotSupportedException>(query.ToSqlCommand);

            Assert.Equal(
                "The query needs 5 SQLite parameters, but this connection allows 3. " +
                "Reduce the number of separate parameter values in the query.",
                exception.Message);
        }
        finally
        {
            raw.sqlite3_limit(db.Handle!, raw.SQLITE_LIMIT_VARIABLE_NUMBER, oldLimit);
        }
    }

    [Fact]
    public void SpanContainsOverANullArrayMatchesAnEmptySpan()
    {
        using TestDatabase db = Create();
        int[]? values = null;

        List<int> expected = Data
            .Where(x => MemoryExtensions.Contains<int>(values!, x.V))
            .Select(x => x.Id)
            .ToList();
        List<int> actual = db.Table<IalRow>()
            .Where(x => MemoryExtensions.Contains<int>(values!, x.V))
            .Select(x => x.Id)
            .ToList();

        Assert.Empty(expected);
        Assert.Equal(expected, actual);
    }

    private static int Identity(int value)
    {
        return value;
    }
}
