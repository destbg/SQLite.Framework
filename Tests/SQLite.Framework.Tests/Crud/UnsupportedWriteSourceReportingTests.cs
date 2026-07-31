using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26zWriteSourceRows")]
public class H26zWriteSourceRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Amount { get; set; }
}

public class UnsupportedWriteSourceReportingTests
{
    [Fact]
    public void DeletingThroughACommonTableExpressionSourceReportsAnUnsupportedSource()
    {
        using TestDatabase db = Setup(nameof(DeletingThroughACommonTableExpressionSourceReportsAnUnsupportedSource));

        Assert.Throws<NotSupportedException>(() => db
            .With(() => db.Table<H26zWriteSourceRow>().Where(r => r.Amount > 5))
            .Where(r => r.Id > 0)
            .ExecuteDelete());
    }

    [Fact]
    public void UpdatingThroughACommonTableExpressionSourceReportsAnUnsupportedSource()
    {
        using TestDatabase db = Setup(nameof(UpdatingThroughACommonTableExpressionSourceReportsAnUnsupportedSource));

        Assert.Throws<NotSupportedException>(() => db
            .With(() => db.Table<H26zWriteSourceRow>().Where(r => r.Amount > 5))
            .Where(r => r.Id > 0)
            .ExecuteUpdate(s => s.Set(r => r.Amount, 1)));
    }

    [Fact]
    public void DeletingThroughAValuesSourceReportsAnUnsupportedSource()
    {
        using TestDatabase db = Setup(nameof(DeletingThroughAValuesSourceReportsAnUnsupportedSource));

        Assert.Throws<NotSupportedException>(() => db
            .ValuesRange(new List<int> { 1, 2 })
            .Where(v => v > 0)
            .ExecuteDelete());
    }

    [Fact]
    public void AUserDefinedValuesOperatorIsNotTreatedAsAValuesSource()
    {
        using TestDatabase db = Setup(nameof(AUserDefinedValuesOperatorIsNotTreatedAsAValuesSource));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<H26zWriteSourceRow>()
            .Values(1)
            .ExecuteDelete());

        Assert.DoesNotContain("values list", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DeletingThroughASingleValueSourceReportsAnUnsupportedSource()
    {
        using TestDatabase db = Setup(nameof(DeletingThroughASingleValueSourceReportsAnUnsupportedSource));

        Assert.Throws<NotSupportedException>(() => db
            .Values(1)
            .Where(v => v > 0)
            .ExecuteDelete());
    }

    [Fact]
    public void DeletingThroughAGroupedSourceReportsAnUnsupportedSource()
    {
        using TestDatabase db = Setup(nameof(DeletingThroughAGroupedSourceReportsAnUnsupportedSource));

        Assert.Throws<NotSupportedException>(() => db.Table<H26zWriteSourceRow>()
            .GroupBy(r => r.Amount)
            .Select(g => g.Key)
            .Where(k => k > 0)
            .ExecuteDelete());
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26zWriteSourceRow>().Schema.CreateTable();
        db.Table<H26zWriteSourceRow>().AddRange(
        [
            new H26zWriteSourceRow { Id = 1, Name = "a", Amount = 10 },
            new H26zWriteSourceRow { Id = 2, Name = "b", Amount = 20 }
        ]);
        return db;
    }
}

public static class UserDefinedValuesOperatorExtensions
{
    public static IQueryable<T> Values<T>(this IQueryable<T> source, int marker)
    {
        return source.Provider.CreateQuery<T>(System.Linq.Expressions.Expression.Call(
            new Func<IQueryable<T>, int, IQueryable<T>>(Values).Method,
            source.Expression,
            System.Linq.Expressions.Expression.Constant(marker)));
    }
}
