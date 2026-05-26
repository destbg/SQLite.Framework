using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

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
                    .Rows(SQLiteFrameBoundary.UnboundedPreceding(), SQLiteFrameBoundary.CurrentRow())
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
                    .Rows(SQLiteFrameBoundary.UnboundedPreceding(), SQLiteFrameBoundary.CurrentRow())
            })
            .OrderBy(r => r.Id)
            .ToList();

        Assert.Equal(5, results.Count);
        Assert.Equal(1L, results[0].RowNum);
        Assert.Equal(5L, results[4].RowNum);
    }

    [Fact]
    public void Avg_Min_Max_Count_AllProduceCorrectSql()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = db.Table<Order>()
            .Select(o => new
            {
                o.Id,
                Avg = SQLiteWindowFunctions.Avg(o.Amount).PartitionBy(o.CustomerId),
                Min = SQLiteWindowFunctions.Min(o.Amount).PartitionBy(o.CustomerId),
                Max = SQLiteWindowFunctions.Max(o.Amount).PartitionBy(o.CustomerId),
                CntAll = SQLiteWindowFunctions.Count(),
                CntCol = SQLiteWindowFunctions.Count(o.Amount),
            })
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT w0.Id AS "Id",
                            AVG(w0.Amount) OVER ( PARTITION BY w0.CustomerId) AS "Avg",
                            MIN(w0.Amount) OVER ( PARTITION BY w0.CustomerId) AS "Min",
                            MAX(w0.Amount) OVER ( PARTITION BY w0.CustomerId) AS "Max",
                            COUNT(*) OVER () AS "CntAll",
                            COUNT(w0.Amount) OVER () AS "CntCol"
                     FROM "Order" AS w0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Ranking_PercentRank_CumeDist_NTile_DenseRank_ProduceCorrectSql()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = db.Table<Order>()
            .Select(o => new
            {
                o.Id,
                Pr = SQLiteWindowFunctions.PercentRank().OrderBy(o.Amount),
                Cd = SQLiteWindowFunctions.CumeDist().OrderBy(o.Amount),
                Nt = SQLiteWindowFunctions.NTile(4).OrderBy(o.Amount),
                Dr = SQLiteWindowFunctions.DenseRank().OrderBy(o.Amount),
            })
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT w0.Id AS "Id",
                            PERCENT_RANK() OVER ( ORDER BY w0.Amount ASC) AS "Pr",
                            CUME_DIST() OVER ( ORDER BY w0.Amount ASC) AS "Cd",
                            NTILE(@p0) OVER ( ORDER BY w0.Amount ASC) AS "Nt",
                            DENSE_RANK() OVER ( ORDER BY w0.Amount ASC) AS "Dr"
                     FROM "Order" AS w0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Lead_AllOverloads_FirstValue_LastValue_NthValue_ProduceCorrectSql()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = db.Table<Order>()
            .Select(o => new
            {
                o.Id,
                Lead1 = SQLiteWindowFunctions.Lead(o.Amount).OrderBy(o.Id),
                Lead2 = SQLiteWindowFunctions.Lead(o.Amount, 1L).OrderBy(o.Id),
                Lead3 = SQLiteWindowFunctions.Lead(o.Amount, 1L, 0.0).OrderBy(o.Id),
                Fv = SQLiteWindowFunctions.FirstValue(o.Amount).OrderBy(o.Id),
                Lv = SQLiteWindowFunctions.LastValue(o.Amount).OrderBy(o.Id),
                Nv = SQLiteWindowFunctions.NthValue(o.Amount, 2L).OrderBy(o.Id),
            })
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT w0.Id AS "Id",
                            LEAD(w0.Amount) OVER ( ORDER BY w0.Id ASC) AS "Lead1",
                            LEAD(w0.Amount, @p0) OVER ( ORDER BY w0.Id ASC) AS "Lead2",
                            LEAD(w0.Amount, @p1, @p2) OVER ( ORDER BY w0.Id ASC) AS "Lead3",
                            FIRST_VALUE(w0.Amount) OVER ( ORDER BY w0.Id ASC) AS "Fv",
                            LAST_VALUE(w0.Amount) OVER ( ORDER BY w0.Id ASC) AS "Lv",
                            NTH_VALUE(w0.Amount, @p3) OVER ( ORDER BY w0.Id ASC) AS "Nv"
                     FROM "Order" AS w0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Frame_Range_Groups_ThenOrderBy_ThenPartitionBy_ProduceCorrectSql()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = db.Table<Order>()
            .Select(o => new
            {
                o.Id,
                R = SQLiteWindowFunctions.Sum(o.Amount)
                    .PartitionBy(o.CustomerId)
                    .ThenPartitionBy(o.Date)
                    .OrderBy(o.Id)
                    .ThenOrderBy(o.Date)
                    .ThenOrderByDescending(o.Amount)
                    .Range(SQLiteFrameBoundary.UnboundedPreceding(), SQLiteFrameBoundary.CurrentRow()),
                G = SQLiteWindowFunctions.Sum(o.Amount)
                    .OrderBy(o.Id)
                    .Groups(SQLiteFrameBoundary.Preceding(2), SQLiteFrameBoundary.Following(1)),
                Rw = SQLiteWindowFunctions.Sum(o.Amount)
                    .OrderByDescending(o.Id)
                    .Rows(SQLiteFrameBoundary.UnboundedPreceding(), SQLiteFrameBoundary.UnboundedFollowing()),
            })
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT w0.Id AS "Id",
                            SUM(w0.Amount) OVER ( PARTITION BY w0.CustomerId, w0.Date ORDER BY w0.Id ASC, w0.Date ASC, w0.Amount DESC RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS "R",
                            SUM(w0.Amount) OVER ( ORDER BY w0.Id ASC GROUPS BETWEEN @p0 PRECEDING AND @p1 FOLLOWING) AS "G",
                            SUM(w0.Amount) OVER ( ORDER BY w0.Id DESC ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) AS "Rw"
                     FROM "Order" AS w0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    private static TestDatabase SetupDatabase(Action<SQLiteOptionsBuilder>? configure = null, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(b =>
        {
            configure?.Invoke(b);
        }, methodName);
        db.Table<Order>().Schema.CreateTable();
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

    [Fact]
    public void Over_LegacyShim_StillProducesCorrectSql()
    {
        using TestDatabase db = SetupDatabase();

        SQLiteCommand command = db.Table<Order>()
            .Select(o => new OrderWithRowNum
            {
                Id = o.Id,
                RowNum = SQLiteWindowFunctions.RowNumber().Over().OrderBy(o.Id),
            })
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT w0.Id AS "Id",
                            ROW_NUMBER() OVER ( ORDER BY w0.Id ASC) AS "RowNum"
                     FROM "Order" AS w0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
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
