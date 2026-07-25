using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21lRangeRollbacks")]
public class H21lRangeRollbackRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Indexed(IsUnique = true)]
    public string Name { get; set; } = "";
}

public class AsyncRangeWriteFailureKeyStateTests
{
    [Fact]
    public async Task FailedAddRangeAsyncLeavesEntitiesWithoutKeys()
    {
        using TestDatabase db = new();
        db.Table<H21lRangeRollbackRow>().Schema.CreateTable();
        List<H21lRangeRollbackRow> items = Items();

        await Assert.ThrowsAsync<SQLiteException>(async () =>
            await db.Table<H21lRangeRollbackRow>().AddRangeAsync(items, ct: TestContext.Current.CancellationToken));

        Assert.Equal(0, db.Table<H21lRangeRollbackRow>().Count());
        Assert.Equal(0, items[0].Id);
    }

    [Fact]
    public async Task FailedAddRangeAsyncWithActionHookLeavesEntitiesWithoutKeys()
    {
        using TestDatabase db = new(b => b.OnAction((d, entity, action) => action));
        db.Table<H21lRangeRollbackRow>().Schema.CreateTable();
        List<H21lRangeRollbackRow> items = Items();

        await Assert.ThrowsAsync<SQLiteException>(async () =>
            await db.Table<H21lRangeRollbackRow>().AddRangeAsync(items, ct: TestContext.Current.CancellationToken));

        Assert.Equal(0, db.Table<H21lRangeRollbackRow>().Count());
        Assert.Equal(0, items[0].Id);
    }

    [Fact]
    public async Task FailedAddRangeAsyncWithColumnHookLeavesEntitiesWithoutKeys()
    {
        using TestDatabase db = new(b => b.OnAdd<H21lRangeRollbackRow>((d, row, columns) =>
        {
            columns["Name"] = row.Name;
            return true;
        }));
        db.Table<H21lRangeRollbackRow>().Schema.CreateTable();
        List<H21lRangeRollbackRow> items = Items();

        await Assert.ThrowsAsync<SQLiteException>(async () =>
            await db.Table<H21lRangeRollbackRow>().AddRangeAsync(items, ct: TestContext.Current.CancellationToken));

        Assert.Equal(0, db.Table<H21lRangeRollbackRow>().Count());
        Assert.Equal(0, items[0].Id);
    }

    [Fact]
    public async Task SuccessfulAddRangeAsyncAssignsKeys()
    {
        using TestDatabase db = new();
        db.Table<H21lRangeRollbackRow>().Schema.CreateTable();
        List<H21lRangeRollbackRow> items =
        [
            new H21lRangeRollbackRow { Name = "a" },
            new H21lRangeRollbackRow { Name = "b" }
        ];

        await db.Table<H21lRangeRollbackRow>().AddRangeAsync(items, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, db.Table<H21lRangeRollbackRow>().Count());
        Assert.Equal(1, items[0].Id);
        Assert.Equal(2, items[1].Id);
    }

    private static List<H21lRangeRollbackRow> Items()
    {
        return
        [
            new H21lRangeRollbackRow { Name = "a" },
            new H21lRangeRollbackRow { Name = "b" },
            new H21lRangeRollbackRow { Name = "a" }
        ];
    }
}
