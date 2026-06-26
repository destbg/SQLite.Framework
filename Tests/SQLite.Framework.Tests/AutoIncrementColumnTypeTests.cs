using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AutoIncrementColumnTypeTests
{
    [Table("StringAutoInc")]
    public class StringKeyAutoIncrement
    {
        [Key]
        [AutoIncrement]
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }

    [Table("IntAutoInc")]
    public class IntKeyAutoIncrement
    {
        [Key]
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [Table("NonKeyAutoInc")]
    public class NonKeyAutoIncrement
    {
        [Key]
        public int Id { get; set; }
        [AutoIncrement]
        public int Counter { get; set; }
        public string Name { get; set; } = "";
    }

    [Table("WithoutRowIdAutoInc")]
    [WithoutRowId]
    public class WithoutRowIdAutoIncrement
    {
        [Key]
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [Fact]
    public void NonIntegerAutoIncrementKey_ThrowsClearError()
    {
        using TestDatabase db = new();
        Assert.Throws<InvalidOperationException>(() => db.Table<StringKeyAutoIncrement>().Schema.CreateTable());
    }

    [Fact]
    public void IntegerAutoIncrementKey_CreatesAndAssignsKeys()
    {
        using TestDatabase db = new();
        db.Table<IntKeyAutoIncrement>().Schema.CreateTable();
        IntKeyAutoIncrement row = new() { Name = "a" };
        db.Table<IntKeyAutoIncrement>().Add(row);
        Assert.Equal(1, row.Id);
    }

    [Fact]
    public void NonKeyAutoIncrementColumn_ThrowsClearError()
    {
        using TestDatabase db = new();
        Assert.Throws<InvalidOperationException>(() => db.Table<NonKeyAutoIncrement>().Schema.CreateTable());
    }

    [Fact]
    public void WithoutRowIdAutoIncrementKey_ThrowsClearError()
    {
        using TestDatabase db = new();
        Assert.Throws<InvalidOperationException>(() => db.Table<WithoutRowIdAutoIncrement>().Schema.CreateTable());
    }
}
