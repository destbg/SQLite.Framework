using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Window;

namespace SQLite.Framework.Tests;

public class WindowFunctionTests
{
    [Fact]
    public void RowNumber_Over_OrderBy_ProducesCorrectSql()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = db.Table<Order>()
            .Select(o => new OrderWithRowNum
            {
                Id = o.Id,
                RowNum = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .OrderBy(o.Id)
            })
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT w0.Id AS "Id",
                            ROW_NUMBER() OVER ( ORDER BY w0.Id ASC) AS "RowNum"
                     FROM "Order" AS w0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Sum_Over_PartitionBy_OrderBy_ProducesCorrectSql()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = db.Table<Order>()
            .Select(o => new OrderWithTotal
            {
                Id = o.Id,
                RunningTotal = SQLiteWindowFunctions.Sum(o.Amount)
                    .Over()
                    .PartitionBy(o.CustomerId)
                    .OrderBy(o.Date)
            })
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT w0.Id AS "Id",
                            SUM(w0.Amount) OVER ( PARTITION BY w0.CustomerId ORDER BY w0.Date ASC) AS "RunningTotal"
                     FROM "Order" AS w0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Rank_Over_PartitionBy_OrderByDescending_ProducesCorrectSql()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = db.Table<Order>()
            .Select(o => new OrderWithRowNum
            {
                Id = o.Id,
                RowNum = SQLiteWindowFunctions.Rank()
                    .Over()
                    .PartitionBy(o.CustomerId)
                    .OrderByDescending(o.Amount)
            })
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT w0.Id AS "Id",
                            RANK() OVER ( PARTITION BY w0.CustomerId ORDER BY w0.Amount DESC) AS "RowNum"
                     FROM "Order" AS w0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Lag_Over_OrderBy_ProducesCorrectSql()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = db.Table<Order>()
            .Select(o => new OrderWithTotal
            {
                Id = o.Id,
                RunningTotal = SQLiteWindowFunctions.Lag(o.Amount, 1L, 0.0)
                    .Over()
                    .OrderBy(o.Id)
            })
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT w0.Id AS "Id",
                            LAG(w0.Amount, @p0, @p1) OVER ( ORDER BY w0.Id ASC) AS "RunningTotal"
                     FROM "Order" AS w0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Rows_WithFrameBoundary_ProducesCorrectSql()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = db.Table<Order>()
            .Select(o => new OrderWithTotal
            {
                Id = o.Id,
                RunningTotal = SQLiteWindowFunctions.Sum(o.Amount)
                    .Over()
                    .OrderBy(o.Id)
                    .Rows(FrameBoundary.UnboundedPreceding(), FrameBoundary.CurrentRow())
            })
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT w0.Id AS "Id",
                            SUM(w0.Amount) OVER ( ORDER BY w0.Id ASC ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS "RunningTotal"
                     FROM "Order" AS w0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ThenPartitionBy_ProducesCorrectSql()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = db.Table<Order>()
            .Select(o => new OrderWithRowNum
            {
                Id = o.Id,
                RowNum = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .PartitionBy(o.CustomerId)
                    .ThenPartitionBy(o.Amount)
                    .OrderBy(o.Id)
            })
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT w0.Id AS "Id",
                            ROW_NUMBER() OVER ( PARTITION BY w0.CustomerId, w0.Amount ORDER BY w0.Id ASC) AS "RowNum"
                     FROM "Order" AS w0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ThenOrderByDescending_ProducesCorrectSql()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = db.Table<Order>()
            .Select(o => new OrderWithRowNum
            {
                Id = o.Id,
                RowNum = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .OrderBy(o.CustomerId)
                    .ThenOrderByDescending(o.Amount)
            })
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT w0.Id AS "Id",
                            ROW_NUMBER() OVER ( ORDER BY w0.CustomerId ASC, w0.Amount DESC) AS "RowNum"
                     FROM "Order" AS w0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void RowNumber_ReturnsCorrectRanking()
    {
        using TestDatabase db = SetupDatabase();

        List<OrderWithRowNum> results = db.Table<Order>()
            .Select(o => new OrderWithRowNum
            {
                Id = o.Id,
                RowNum = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .OrderBy(o.Id)
            })
            .OrderBy(r => r.Id)
            .ToList();

        Assert.Equal(5, results.Count);
        Assert.Equal(1L, results[0].RowNum);
        Assert.Equal(2L, results[1].RowNum);
        Assert.Equal(3L, results[2].RowNum);
        Assert.Equal(4L, results[3].RowNum);
        Assert.Equal(5L, results[4].RowNum);
    }

    [Fact]
    public void Sum_RunningTotal_ReturnsCorrectValues()
    {
        using TestDatabase db = SetupDatabase();

        List<OrderWithTotal> results = db.Table<Order>()
            .Where(o => o.CustomerId == 1)
            .Select(o => new OrderWithTotal
            {
                Id = o.Id,
                RunningTotal = SQLiteWindowFunctions.Sum(o.Amount)
                    .Over()
                    .PartitionBy(o.CustomerId)
                    .OrderBy(o.Id)
            })
            .OrderBy(r => r.Id)
            .ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal(100.0, results[0].RunningTotal);
        Assert.Equal(300.0, results[1].RunningTotal);
        Assert.Equal(600.0, results[2].RunningTotal);
    }

    [Fact]
    public void Rank_WithTies_ReturnsCorrectRanks()
    {
        using TestDatabase db = SetupDatabase();

        List<OrderWithRowNum> results = db.Table<Order>()
            .Select(o => new OrderWithRowNum
            {
                Id = o.Id,
                RowNum = SQLiteWindowFunctions.Rank()
                    .Over()
                    .PartitionBy(o.CustomerId)
                    .OrderByDescending(o.Amount)
            })
            .OrderBy(r => r.Id)
            .ToList();

        Assert.Equal(5, results.Count);
        Assert.Equal(3L, results[0].RowNum);
        Assert.Equal(2L, results[1].RowNum);
        Assert.Equal(2L, results[2].RowNum);
        Assert.Equal(1L, results[3].RowNum);
        Assert.Equal(1L, results[4].RowNum);
    }

    [Fact]
    public void Lag_ReturnsCorrectPreviousValue()
    {
        using TestDatabase db = SetupDatabase();

        List<OrderWithTotal> results = db.Table<Order>()
            .Select(o => new OrderWithTotal
            {
                Id = o.Id,
                RunningTotal = SQLiteWindowFunctions.Lag(o.Amount, 1L, 0.0)
                    .Over()
                    .OrderBy(o.Id)
            })
            .OrderBy(r => r.Id)
            .ToList();

        Assert.Equal(5, results.Count);
        Assert.Equal(0.0, results[0].RunningTotal);
        Assert.Equal(100.0, results[1].RunningTotal);
        Assert.Equal(200.0, results[2].RunningTotal);
    }

    [Fact]
    public void DenseRank_ReturnsCorrectValues()
    {
        using TestDatabase db = SetupDatabase();

        List<OrderWithRowNum> results = db.Table<Order>()
            .Select(o => new OrderWithRowNum
            {
                Id = o.Id,
                RowNum = SQLiteWindowFunctions.DenseRank()
                    .Over()
                    .PartitionBy(o.CustomerId)
                    .OrderBy(o.Amount)
            })
            .OrderBy(r => r.Id)
            .ToList();

        Assert.Equal(5, results.Count);
        Assert.All(results, r => Assert.True(r.RowNum >= 1));
    }

    [Fact]
    public void RowNumber_WithRowsFrame_ReturnsCorrectValues()
    {
        using TestDatabase db = SetupDatabase();

        List<OrderWithRowNum> results = db.Table<Order>()
            .Select(o => new OrderWithRowNum
            {
                Id = o.Id,
                RowNum = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .OrderBy(o.Id)
                    .Rows(FrameBoundary.UnboundedPreceding(), FrameBoundary.CurrentRow())
            })
            .OrderBy(r => r.Id)
            .ToList();

        Assert.Equal(5, results.Count);
        Assert.Equal(1L, results[0].RowNum);
        Assert.Equal(5L, results[4].RowNum);
    }

    private static TestDatabase SetupDatabase(Action<SQLiteOptionsBuilder>? configure = null, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(b =>
        {
            b.AddWindow();
            configure?.Invoke(b);
        }, methodName);
        db.Schema.CreateTable<Order>();
        db.Table<Order>().AddRange([
            new Order
            {
                CustomerId = 1,
                Amount = 100.0,
                Date = new DateTime(2024, 1, 1)
            },
            new Order
            {
                CustomerId = 1,
                Amount = 200.0,
                Date = new DateTime(2024, 1, 2)
            },
            new Order
            {
                CustomerId = 2,
                Amount = 150.0,
                Date = new DateTime(2024, 1, 1)
            },
            new Order
            {
                CustomerId = 2,
                Amount = 250.0,
                Date = new DateTime(2024, 1, 2)
            },
            new Order
            {
                CustomerId = 1,
                Amount = 300.0,
                Date = new DateTime(2024, 1, 3)
            },
        ]);
        return db;
    }
}

[Table("Order")]
file class Order
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public double Amount { get; set; }
    public DateTime Date { get; set; }
}

file class OrderWithRowNum
{
    public int Id { get; set; }
    public long RowNum { get; set; }
}

file class OrderWithTotal
{
    public int Id { get; set; }
    public double RunningTotal { get; set; }
}