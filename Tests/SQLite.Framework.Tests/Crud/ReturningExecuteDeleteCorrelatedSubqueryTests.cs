using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24nAliasOrderRows")]
public class H24nAliasOrderRow
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

[Table("H24nAliasPaymentRows")]
public class H24nAliasPaymentRow
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }
}

public class ReturningExecuteDeleteCorrelatedSubqueryTests
{
    [Fact]
    public void ReturningExecuteDeleteResolvesTheOuterColumnAgainstTheTargetTable()
    {
        using TestDatabase db = Setup(nameof(ReturningExecuteDeleteResolvesTheOuterColumnAgainstTheTargetTable));

        List<int> expected = Orders()
            .Where(o => Payments().Any(p => p.OrderId == o.Id))
            .Select(o => o.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<H24nAliasOrderRow>()
            .Where(o => db.Table<H24nAliasPaymentRow>().Any(p => p.OrderId == o.Id))
            .Returning(o => o.Id)
            .ExecuteDelete();

        Assert.Equal(expected, actual.OrderBy(i => i).ToList());
    }

    [Fact]
    public void ReturningExecuteDeleteLeavesTheUnmatchedRowsInPlace()
    {
        using TestDatabase db = Setup(nameof(ReturningExecuteDeleteLeavesTheUnmatchedRowsInPlace));

        db.Table<H24nAliasOrderRow>()
            .Where(o => db.Table<H24nAliasPaymentRow>().Any(p => p.OrderId == o.Id))
            .Returning(o => o.Id)
            .ExecuteDelete();

        List<int> expected = Orders()
            .Where(o => !Payments().Any(p => p.OrderId == o.Id))
            .Select(o => o.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = db.Table<H24nAliasOrderRow>()
            .OrderBy(o => o.Id)
            .Select(o => o.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24nAliasOrderRow> Orders()
    {
        return
        [
            new H24nAliasOrderRow { Id = 1, Code = "a" },
            new H24nAliasOrderRow { Id = 2, Code = "b" },
            new H24nAliasOrderRow { Id = 3, Code = "c" },
            new H24nAliasOrderRow { Id = 4, Code = "d" }
        ];
    }

    private static List<H24nAliasPaymentRow> Payments()
    {
        return
        [
            new H24nAliasPaymentRow { Id = 10, OrderId = 2 },
            new H24nAliasPaymentRow { Id = 11, OrderId = 3 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24nAliasOrderRow>().Schema.CreateTable();
        db.Table<H24nAliasPaymentRow>().Schema.CreateTable();
        db.Table<H24nAliasOrderRow>().AddRange(Orders());
        db.Table<H24nAliasPaymentRow>().AddRange(Payments());
        return db;
    }
}
