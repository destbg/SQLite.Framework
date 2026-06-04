using System;
using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class OrderByChainSemanticsTests
{
    [Fact]
    public void ChainedOrderByKeepsOnlyTheLastKey()
    {
        using TestDatabase db = new();
        db.Table<TwoNullableIntEntity>().Schema.CreateTable();

        string sql = db.Table<TwoNullableIntEntity>()
            .OrderBy(x => x.A)
            .OrderBy(x => x.B)
            .ToSqlCommand()
            .CommandText;

        string orderBy = sql[sql.IndexOf("ORDER BY", StringComparison.Ordinal)..];
        Assert.Contains("\"B\"", orderBy);
        Assert.DoesNotContain("\"A\"", orderBy);
    }

    [Fact]
    public void ChainedOrderByDescendingKeepsOnlyTheLastKey()
    {
        using TestDatabase db = new();
        db.Table<TwoNullableIntEntity>().Schema.CreateTable();

        string sql = db.Table<TwoNullableIntEntity>()
            .OrderByDescending(x => x.A)
            .OrderByDescending(x => x.B)
            .ToSqlCommand()
            .CommandText;

        string orderBy = sql[sql.IndexOf("ORDER BY", StringComparison.Ordinal)..];
        Assert.Contains("\"B\" DESC", orderBy);
        Assert.DoesNotContain("\"A\"", orderBy);
    }
}
