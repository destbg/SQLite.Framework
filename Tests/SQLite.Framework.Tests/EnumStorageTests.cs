using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EnumStorageTests
{
    [Fact]
    public void Integer_RoundTrip()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Status = TestStatus.Active
        });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(TestStatus.Active, result.Status);
    }

    [Fact]
    public void Integer_RoundTrip_AllValues()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Status = TestStatus.Active
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Status = TestStatus.Inactive
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 3,
            Status = TestStatus.Pending
        });

        List<TestEntity> results = db.Table<TestEntity>().OrderBy(a => a.Id).ToList();

        Assert.Equal(TestStatus.Active, results[0].Status);
        Assert.Equal(TestStatus.Inactive, results[1].Status);
        Assert.Equal(TestStatus.Pending, results[2].Status);
    }

    [Fact]
    public void Integer_Where_EnumEquals()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Status = TestStatus.Active
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Status = TestStatus.Inactive
        });

        List<TestEntity> results = db.Table<TestEntity>().Where(a => a.Status == TestStatus.Active).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void Integer_Where_EnumNotEquals()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Status = TestStatus.Active
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Status = TestStatus.Inactive
        });

        List<TestEntity> results = db.Table<TestEntity>().Where(a => a.Status != TestStatus.Active).ToList();

        Assert.Single(results);
        Assert.Equal(2, results[0].Id);
    }

    [Fact]
    public void Text_RoundTrip()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.EnumStorage = EnumStorageMode.Text;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Status = TestStatus.Active
        });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(TestStatus.Active, result.Status);
    }

    [Fact]
    public void Text_RoundTrip_AllValues()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.EnumStorage = EnumStorageMode.Text;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Status = TestStatus.Active
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Status = TestStatus.Inactive
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 3,
            Status = TestStatus.Pending
        });

        List<TestEntity> results = db.Table<TestEntity>().OrderBy(a => a.Id).ToList();

        Assert.Equal(TestStatus.Active, results[0].Status);
        Assert.Equal(TestStatus.Inactive, results[1].Status);
        Assert.Equal(TestStatus.Pending, results[2].Status);
    }

    [Fact]
    public void Text_Where_EnumEquals()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.EnumStorage = EnumStorageMode.Text;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Status = TestStatus.Active
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Status = TestStatus.Inactive
        });

        List<TestEntity> results = db.Table<TestEntity>().Where(a => a.Status == TestStatus.Active).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void Text_Where_EnumNotEquals()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.EnumStorage = EnumStorageMode.Text;
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 1,
            Status = TestStatus.Active
        });
        db.Table<TestEntity>().Add(new TestEntity
        {
            Id = 2,
            Status = TestStatus.Inactive
        });

        List<TestEntity> results = db.Table<TestEntity>().Where(a => a.Status != TestStatus.Active).ToList();

        Assert.Single(results);
        Assert.Equal(2, results[0].Id);
    }

    [Fact]
    public void Text_UnknownValue_ReturnsDefault()
    {
        using TestDatabase db = SetupDatabase(b =>
        {
            b.EnumStorage = EnumStorageMode.Text;
        });
        db.Execute("INSERT INTO TestEntity (Id, Status) VALUES (1, @status)",
            new SQLiteParameter
            {
                Name = "@status",
                Value = "UnknownStatus"
            });

        TestEntity result = db.Table<TestEntity>().First();

        Assert.Equal(default, result.Status);
    }

    private static TestDatabase SetupDatabase(Action<SQLiteOptionsBuilder>? configure = null, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(configure, methodName);
        db.Table<TestEntity>().Schema.CreateTable();
        return db;
    }

    private enum TestStatus
    {
        Active = 1,
        Inactive = 2,
        Pending = 3
    }

    private class TestEntity
    {
        [Key]
        public required int Id { get; set; }

        public required TestStatus Status { get; set; }
    }
}