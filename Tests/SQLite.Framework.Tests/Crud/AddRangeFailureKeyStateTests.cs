using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RangeRollback")]
public class RangeRollbackRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Indexed(IsUnique = true)]
    public string Name { get; set; } = "";
}

public class AddRangeFailureKeyStateTests
{
    private static List<RangeRollbackRow> Items() =>
    [
        new() { Name = "a" },
        new() { Name = "b" },
        new() { Name = "a" },
    ];

    [Fact]
    public void FailedAddRangeLeavesEntitiesWithoutKeys()
    {
        using TestDatabase db = new();
        db.Table<RangeRollbackRow>().Schema.CreateTable();
        List<RangeRollbackRow> items = Items();

        Assert.Throws<SQLiteException>(() => db.Table<RangeRollbackRow>().AddRange(items));

        Assert.Equal(0, db.Table<RangeRollbackRow>().Count());
        Assert.Equal(0, items[0].Id);
    }

    [Fact]
    public void FailedAddRangeWithActionHookLeavesEntitiesWithoutKeys()
    {
        using TestDatabase db = new(b => b.OnAction((d, entity, action) => action));
        db.Table<RangeRollbackRow>().Schema.CreateTable();
        List<RangeRollbackRow> items = Items();

        Assert.Throws<SQLiteException>(() => db.Table<RangeRollbackRow>().AddRange(items));

        Assert.Equal(0, db.Table<RangeRollbackRow>().Count());
        Assert.Equal(0, items[0].Id);
    }

    [Fact]
    public void FailedAddRangeWithColumnHookLeavesEntitiesWithoutKeys()
    {
        using TestDatabase db = new(b => b.OnAdd<RangeRollbackRow>((d, row, columns) =>
        {
            columns["Name"] = row.Name;
            return true;
        }));
        db.Table<RangeRollbackRow>().Schema.CreateTable();
        List<RangeRollbackRow> items = Items();

        Assert.Throws<SQLiteException>(() => db.Table<RangeRollbackRow>().AddRange(items));

        Assert.Equal(0, db.Table<RangeRollbackRow>().Count());
        Assert.Equal(0, items[0].Id);
    }
}
