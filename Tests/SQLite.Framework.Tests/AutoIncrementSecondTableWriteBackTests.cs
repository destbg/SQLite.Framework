using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("WriteBackTableA")]
file sealed class WriteBackTableA
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("WriteBackTableB")]
file sealed class WriteBackTableB
{
    [Key]
    [AutoIncrement]
    public long Id { get; set; }

    public string Name { get; set; } = "";
}

public class AutoIncrementSecondTableWriteBackTests
{
    [Fact]
    public void AutoIncrementIdWrittenBackOnSecondTableFirstInsert()
    {
        using TestDatabase db = new();
        db.Table<WriteBackTableA>().Schema.CreateTable();
        db.Table<WriteBackTableB>().Schema.CreateTable();

        WriteBackTableA a = new() { Name = "a" };
        db.Table<WriteBackTableA>().Add(a);

        WriteBackTableB b = new() { Name = "b" };
        db.Table<WriteBackTableB>().Add(b);

        long storedId = db.Query<long>("SELECT \"Id\" FROM \"WriteBackTableB\"").First();

        Assert.Equal(1, storedId);
        Assert.Equal(1, b.Id);
    }

    [Fact]
    public void AutoIncrementIdWrittenBackByAddRangeOnSecondTableFirstInsert()
    {
        using TestDatabase db = new();
        db.Table<WriteBackTableA>().Schema.CreateTable();
        db.Table<WriteBackTableB>().Schema.CreateTable();

        db.Table<WriteBackTableA>().Add(new WriteBackTableA { Name = "a" });

        WriteBackTableB b = new() { Name = "b" };
        db.Table<WriteBackTableB>().AddRange([b]);

        long storedId = db.Query<long>("SELECT \"Id\" FROM \"WriteBackTableB\"").First();

        Assert.Equal(1, storedId);
        Assert.Equal(1, b.Id);
    }
}
