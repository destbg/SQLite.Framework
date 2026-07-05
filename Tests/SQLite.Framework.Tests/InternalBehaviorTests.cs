using System.Linq.Expressions;
using SQLite.Framework;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Visitors;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class QuerySourceHolder
{
    public IQueryable<int> Rows { get; set; } = new List<int>().AsQueryable();

    public SQLiteCte<int>? Names { get; set; }
}

public class InternalBehaviorTests
{
    [Fact]
    public void InlineParametersLeavesALineCommentAtTheEndAlone()
    {
        using TestDatabase db = new();

        string inlined = SqlLiteralHelper.InlineParameters(
            "SELECT @p0 -- tail @p0",
            [new SQLiteParameter { Name = "@p0", Value = 3 }],
            db.Options);

        Assert.Equal("SELECT 3 -- tail @p0", inlined);
    }

    [Fact]
    public void FilterDetectionIgnoresAQueryableMemberOnARowParameter()
    {
        Expression<Func<QuerySourceHolder, bool>> filter = h => h.Rows.Any();

        IgnoreQueryFiltersDetectorVisitor detector = new();
        detector.Visit(filter.Body);

        Assert.False(detector.Found);
    }

    [Fact]
    public void InlineParametersLeavesAnUnterminatedLiteralAlone()
    {
        using TestDatabase db = new();

        string inlined = SqlLiteralHelper.InlineParameters(
            "SELECT @p0, 'abc",
            [new SQLiteParameter { Name = "@p0", Value = 1 }],
            db.Options);

        Assert.Equal("SELECT 1, 'abc", inlined);
    }

    [Fact]
    public void InlineParametersLeavesAnUnterminatedBlockCommentAlone()
    {
        using TestDatabase db = new();

        string inlined = SqlLiteralHelper.InlineParameters(
            "SELECT @p0 /* tail @p0",
            [new SQLiteParameter { Name = "@p0", Value = 2 }],
            db.Options);

        Assert.Equal("SELECT 2 /* tail @p0", inlined);
    }

    [Fact]
    public void FilterDetectionIgnoresACteMemberOnARowParameter()
    {
        Expression<Func<QuerySourceHolder, bool>> filter = h => h.Names != null;

        IgnoreQueryFiltersDetectorVisitor detector = new();
        detector.Visit(filter.Body);

        Assert.False(detector.Found);
    }

    [Fact]
    public void SetReadColumnCollectorSkipsANullColumnName()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping(typeof(SluggedBookRow));
        Expression<Func<SluggedBookRow, string?>> value = x => SQLiteColumn.Of<string?>(x, null!);

        SetReadColumnCollector collector = new(mapping, value.Parameters[0]);
        collector.Visit(value.Body);

        Assert.Empty(collector.Columns);
    }

    [Fact]
    public void SetReadColumnCollectorSkipsNestedAndUnmappedMembers()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping(typeof(ComputedTotalNoteRow));
        Expression<Func<ComputedTotalNoteRow, int>> value = x => x.Doubled + x.Price.ToString().Length;

        SetReadColumnCollector collector = new(mapping, value.Parameters[0]);
        collector.Visit(value.Body);

        Assert.Equal(["Price"], collector.Columns);
    }

    [Fact]
    public void SetReadColumnCollectorSkipsANonConstantColumnName()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping(typeof(SluggedBookRow));
        Expression<Func<SluggedBookRow, string?>> value = x => SQLiteColumn.Of<string?>(x, x.Slug!);

        SetReadColumnCollector collector = new(mapping, value.Parameters[0]);
        collector.Visit(value.Body);

        Assert.Equal(["Slug"], collector.Columns);
    }
}
