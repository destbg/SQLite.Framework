using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ThenPartitionByOrder")]
file sealed class TpbOrder
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public double Amount { get; set; }
}

file sealed class TpbResult
{
    public int Id { get; set; }
    public long RowNum { get; set; }
}

public class ThenPartitionByWithoutPartitionByTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<TpbOrder>().Schema.CreateTable();
        db.Table<TpbOrder>().AddRange(new[]
        {
            new TpbOrder { CustomerId = 1, Amount = 10 },
            new TpbOrder { CustomerId = 1, Amount = 20 },
            new TpbOrder { CustomerId = 2, Amount = 30 },
        });
        return db;
    }

    [Fact]
    public void ThenPartitionBy_WithoutPriorPartitionBy_Throws()
    {
        using TestDatabase db = SetupDatabase();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<TpbOrder>()
                .Select(o => new TpbResult
                {
                    Id = o.Id,
                    RowNum = SQLiteWindowFunctions.RowNumber()
                        .Over()
                        .ThenPartitionBy(o.CustomerId)
                        .OrderBy(o.Id)
                })
                .ToSqlCommand());

        Assert.Contains("ThenPartitionBy", ex.Message);
    }

    [Fact]
    public void ThenOrderBy_WithoutPriorOrderBy_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TpbOrder>()
                .Select(o => new TpbResult
                {
                    Id = o.Id,
                    RowNum = SQLiteWindowFunctions.RowNumber()
                        .Over()
                        .ThenOrderBy(o.CustomerId)
                })
                .ToSqlCommand());
    }

    [Fact]
    public void ThenOrderByDescending_WithoutPriorOrderBy_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TpbOrder>()
                .Select(o => new TpbResult
                {
                    Id = o.Id,
                    RowNum = SQLiteWindowFunctions.RowNumber()
                        .Over()
                        .ThenOrderByDescending(o.CustomerId)
                })
                .ToSqlCommand());
    }

    [Fact]
    public void ThenPartitionBy_AfterOrderBy_Throws()
    {
        using TestDatabase db = SetupDatabase();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<TpbOrder>()
                .Select(o => new TpbResult
                {
                    Id = o.Id,
                    RowNum = SQLiteWindowFunctions.RowNumber()
                        .Over()
                        .PartitionBy(o.CustomerId)
                        .OrderBy(o.Id)
                        .ThenPartitionBy(o.Amount)
                })
                .ToSqlCommand());
    }

    [Fact]
    public void ThenPartitionBy_AfterThenPartitionBy_Works()
    {
        using TestDatabase db = SetupDatabase();

        List<TpbResult> results = db.Table<TpbOrder>()
            .Select(o => new TpbResult
            {
                Id = o.Id,
                RowNum = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .PartitionBy(o.CustomerId)
                    .ThenPartitionBy(o.Amount)
                    .ThenPartitionBy(o.Id)
                    .OrderBy(o.Id)
            })
            .OrderBy(r => r.Id)
            .ToList();

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal(1L, r.RowNum));
    }

    [Fact]
    public void ThenOrderBy_MixedChain_Works()
    {
        using TestDatabase db = SetupDatabase();

        List<TpbResult> results = db.Table<TpbOrder>()
            .Select(o => new TpbResult
            {
                Id = o.Id,
                RowNum = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .OrderBy(o.CustomerId)
                    .ThenOrderBy(o.Amount)
                    .ThenOrderByDescending(o.Id)
                    .ThenOrderBy(o.Amount)
            })
            .OrderBy(r => r.Id)
            .ToList();

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void ThenPartitionBy_AfterFilterAfterPartitionBy_Works()
    {
        using TestDatabase db = SetupDatabase();

        List<double> results = db.Table<TpbOrder>()
            .Select(o => (double)SQLiteWindowFunctions.Sum(o.Amount)
                .Over()
                .PartitionBy(o.CustomerId)
                .Filter(o.Amount > 0)
                .ThenPartitionBy(o.Id))
            .ToList();

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void ThenOrderBy_AfterOrderByDescending_Works()
    {
        using TestDatabase db = SetupDatabase();

        List<TpbResult> results = db.Table<TpbOrder>()
            .Select(o => new TpbResult
            {
                Id = o.Id,
                RowNum = SQLiteWindowFunctions.RowNumber()
                    .Over()
                    .OrderByDescending(o.CustomerId)
                    .ThenOrderBy(o.Amount)
            })
            .OrderBy(r => r.Id)
            .ToList();

        Assert.Equal(3, results.Count);
    }
}
