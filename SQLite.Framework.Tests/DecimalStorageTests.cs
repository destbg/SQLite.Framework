using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DecimalStorageTests
{
    [Fact]
    public void Real_RoundTrip()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = 1234.56m
        });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(1234.56m, result.Price, 4);
    }

    [Fact]
    public void Real_Where_Equals()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = 10.00m
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Price = 20.00m
        });

        List<TestEntity> results = db.Table<TestEntity>().Where(a => a.Price == 10.00m).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void Real_Where_GreaterThan()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = 10.00m
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Price = 20.00m
        });

        List<TestEntity> results = db.Table<TestEntity>().Where(a => a.Price > 15.00m).ToList();

        Assert.Single(results);
        Assert.Equal(2, results[0].Id);
    }

    [Fact]
    public void Real_OrderBy()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = 30.00m
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Price = 10.00m
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 3,
            Price = 20.00m
        });

        List<TestEntity> results = db.Table<TestEntity>().OrderBy(a => a.Price).ToList();

        Assert.Equal(2, results[0].Id);
        Assert.Equal(3, results[1].Id);
        Assert.Equal(1, results[2].Id);
    }

    [Fact]
    public void Text_RoundTrip()
    {
        using TestDatabase db = SetupDatabase(null, DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = 1234.56m
        });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(1234.56m, result.Price);
    }

    [Fact]
    public void Text_Select_NoCastInRootProjection()
    {
        using TestDatabase db = SetupDatabase(null, DecimalStorageMode.Text);

        SQLiteCommand command = db.Table<TestEntity>().ToSqlCommand();

        Assert.DoesNotContain("CAST", command.CommandText);
    }

    [Fact]
    public void Text_RoundTrip_HighPrecision()
    {
        using TestDatabase db = SetupDatabase(null, DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = 1234567890.1234567890m
        });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(1234567890.1234567890m, result.Price);
    }

    [Fact]
    public void Text_RoundTrip_Zero()
    {
        using TestDatabase db = SetupDatabase(null, DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = 0m
        });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(0m, result.Price);
    }

    [Fact]
    public void Text_RoundTrip_Negative()
    {
        using TestDatabase db = SetupDatabase(null, DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = -99.99m
        });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(-99.99m, result.Price);
    }

    [Fact]
    public void Text_Where_GreaterThan()
    {
        using TestDatabase db = SetupDatabase(null, DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = 10.00m
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Price = 20.00m
        });

        List<TestEntity> results = db.Table<TestEntity>().Where(a => a.Price > 15.00m).ToList();

        Assert.Single(results);
        Assert.Equal(2, results[0].Id);
    }

    [Fact]
    public void Text_Where_GreaterThan_CastsToReal()
    {
        using TestDatabase db = SetupDatabase(null, DecimalStorageMode.Text);

        SQLiteCommand command = db.Table<TestEntity>().Where(a => a.Price > 15.00m).ToSqlCommand();

        Assert.Equal("""
                     SELECT t0.Id AS "Id",
                            t0.Price AS "Price"
                     FROM "TestEntity" AS t0
                     WHERE CAST(t0.Price AS REAL) > @p0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Text_Where_LessThan()
    {
        using TestDatabase db = SetupDatabase(null, DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = 10.00m
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Price = 20.00m
        });

        List<TestEntity> results = db.Table<TestEntity>().Where(a => a.Price < 15.00m).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void Text_OrderBy()
    {
        using TestDatabase db = SetupDatabase(null, DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = 30.00m
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Price = 10.00m
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 3,
            Price = 20.00m
        });

        List<TestEntity> results = db.Table<TestEntity>().OrderBy(a => a.Price).ToList();

        Assert.Equal(2, results[0].Id);
        Assert.Equal(3, results[1].Id);
        Assert.Equal(1, results[2].Id);
    }

    [Fact]
    public void Text_CustomFormat_RoundTrip()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.DecimalFormat = "F4";
        }, DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = 1234.5678m
        });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(1234.5678m, result.Price);
    }

    private static TestDatabase SetupDatabase(Action<SQLiteOptionsBuilder>? configure = null, DecimalStorageMode storage = DecimalStorageMode.Real, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(b =>
        {
            b.DecimalStorage = storage;
            configure?.Invoke(b);
        }, methodName);
        db.Table<TestEntity>().CreateTable();
        return db;
    }

    [Fact]
    public void Text_SelectContains_ProducesInSubquery()
    {
        using TestDatabase db = SetupDatabase(null, DecimalStorageMode.Text);

        SQLiteCommand command = db.Table<TestEntity>()
            .Select(f => db.Table<TestEntity>().Select(s => s.Price).Contains(f.Price))
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT t0.Price IN (
                         SELECT t1.Price AS "Price"
                         FROM "TestEntity" AS t1
                     ) AS "8"
                     FROM "TestEntity" AS t0
                     """.Replace("\r\n", "\n"),
            command.CommandText.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Text_SelectContains_ReturnsCorrectResults()
    {
        using TestDatabase db = SetupDatabase(null, DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Price = 10.00m
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Price = 20.00m
        });

        List<bool> results = db.Table<TestEntity>()
            .Select(f => db.Table<TestEntity>().Select(s => s.Price).Contains(f.Price))
            .ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r));
    }

    private class TestEntity
    {
        [Key]
        public required int Id { get; set; }

        public required decimal Price { get; set; }
    }
}