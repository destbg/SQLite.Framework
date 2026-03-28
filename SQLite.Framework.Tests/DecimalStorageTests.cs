using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DecimalStorageTests
{
    [Fact]
    public void Real_RoundTrip()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Price = 1234.56m });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(1234.56m, result.Price, 4);
    }

    [Fact]
    public void Real_Where_Equals()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Price = 10.00m });
        db.Table<TestEntity>().Add(new TestEntity { Id = 2, Price = 20.00m });

        List<TestEntity> results = db.Table<TestEntity>().Where(a => a.Price == 10.00m).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void Real_Where_GreaterThan()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Price = 10.00m });
        db.Table<TestEntity>().Add(new TestEntity { Id = 2, Price = 20.00m });

        List<TestEntity> results = db.Table<TestEntity>().Where(a => a.Price > 15.00m).ToList();

        Assert.Single(results);
        Assert.Equal(2, results[0].Id);
    }

    [Fact]
    public void Real_OrderBy()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Price = 30.00m });
        db.Table<TestEntity>().Add(new TestEntity { Id = 2, Price = 10.00m });
        db.Table<TestEntity>().Add(new TestEntity { Id = 3, Price = 20.00m });

        List<TestEntity> results = db.Table<TestEntity>().OrderBy(a => a.Price).ToList();

        Assert.Equal(2, results[0].Id);
        Assert.Equal(3, results[1].Id);
        Assert.Equal(1, results[2].Id);
    }

    [Fact]
    public void Text_RoundTrip()
    {
        using TestDatabase db = SetupDatabase(DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Price = 1234.56m });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(1234.56m, result.Price);
    }

    [Fact]
    public void Text_RoundTrip_HighPrecision()
    {
        using TestDatabase db = SetupDatabase(DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Price = 1234567890.1234567890m });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(1234567890.1234567890m, result.Price);
    }

    [Fact]
    public void Text_RoundTrip_Zero()
    {
        using TestDatabase db = SetupDatabase(DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Price = 0m });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(0m, result.Price);
    }

    [Fact]
    public void Text_RoundTrip_Negative()
    {
        using TestDatabase db = SetupDatabase(DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Price = -99.99m });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(-99.99m, result.Price);
    }

    [Fact]
    public void Text_Where_GreaterThan()
    {
        using TestDatabase db = SetupDatabase(DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Price = 10.00m });
        db.Table<TestEntity>().Add(new TestEntity { Id = 2, Price = 20.00m });

        List<TestEntity> results = db.Table<TestEntity>().Where(a => a.Price > 15.00m).ToList();

        Assert.Single(results);
        Assert.Equal(2, results[0].Id);
    }

    [Fact]
    public void Text_Where_LessThan()
    {
        using TestDatabase db = SetupDatabase(DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Price = 10.00m });
        db.Table<TestEntity>().Add(new TestEntity { Id = 2, Price = 20.00m });

        List<TestEntity> results = db.Table<TestEntity>().Where(a => a.Price < 15.00m).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void Text_OrderBy()
    {
        using TestDatabase db = SetupDatabase(DecimalStorageMode.Text);
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Price = 30.00m });
        db.Table<TestEntity>().Add(new TestEntity { Id = 2, Price = 10.00m });
        db.Table<TestEntity>().Add(new TestEntity { Id = 3, Price = 20.00m });

        List<TestEntity> results = db.Table<TestEntity>().OrderBy(a => a.Price).ToList();

        Assert.Equal(2, results[0].Id);
        Assert.Equal(3, results[1].Id);
        Assert.Equal(1, results[2].Id);
    }

    [Fact]
    public void Text_CustomFormat_RoundTrip()
    {
        using TestDatabase db = SetupDatabase(DecimalStorageMode.Text);
        db.StorageOptions.DecimalFormat = "F4";
        db.Table<TestEntity>().Add(new TestEntity { Id = 1, Price = 1234.5678m });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(1234.5678m, result.Price);
    }

    private static TestDatabase SetupDatabase(DecimalStorageMode storage = DecimalStorageMode.Real, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);
        db.StorageOptions.DecimalStorage = storage;
        db.Table<TestEntity>().CreateTable();
        return db;
    }

    private class TestEntity
    {
        [Key]
        public required int Id { get; set; }

        public required decimal Price { get; set; }
    }
}
