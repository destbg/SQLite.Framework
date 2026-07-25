using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("KeyedBatchEntry")]
public class KeyedBatchEntryRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class RangeWriteKeyStateTests
{
    [Fact]
    public void ReturningAddRangeRollbackClearsAssignedKeys()
    {
        using TestDatabase db = new();
        db.Table<KeyedBatchEntryRow>().Schema.CreateTable();
        KeyedBatchEntryRow first = new() { Name = "a" };
        KeyedBatchEntryRow[] items = [first, new KeyedBatchEntryRow { Name = "b" }, new KeyedBatchEntryRow { Name = null! }];

        Assert.ThrowsAny<Exception>(() => db.Table<KeyedBatchEntryRow>().Returning().AddRange(items));

        Assert.Empty(db.Table<KeyedBatchEntryRow>().ToList());
        Assert.Equal(0, first.Id);
    }

    [Fact]
    public void AddRangeRollbackRestoresTheKeyOfARepeatedInstance()
    {
        using TestDatabase db = new();
        db.Table<KeyedBatchEntryRow>().Schema.CreateTable();
        KeyedBatchEntryRow repeated = new() { Name = "a" };

        Assert.ThrowsAny<Exception>(() => db.Table<KeyedBatchEntryRow>().AddRange([repeated, repeated, new KeyedBatchEntryRow { Name = null! }]));

        Assert.Empty(db.Table<KeyedBatchEntryRow>().ToList());
        Assert.Equal(0, repeated.Id);
    }
}
