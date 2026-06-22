using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class EmptyInsertCounter
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
}

public class ColumnHookEmptyInsertParityTests
{
    [Fact]
    public void KeyOnlyTableWithNoOpColumnHook_AddSucceeds()
    {
        using TestDatabase db = new(b =>
            b.OnAdd<EmptyInsertCounter>((_, _, columns) => true));
        db.Table<EmptyInsertCounter>().Schema.CreateTable();

        EmptyInsertCounter item = new();
        int affected = db.Table<EmptyInsertCounter>().Add(item);

        Assert.Equal(1, affected);
        Assert.Equal(1, item.Id);
    }

    [Fact]
    public void KeyOnlyTableWithNoOpColumnHook_UpdateSucceeds()
    {
        using TestDatabase db = new(b => b.OnUpdate<EmptyInsertCounter>((_, _, columns) => true));
        db.Table<EmptyInsertCounter>().Schema.CreateTable();
        EmptyInsertCounter item = new();
        db.Table<EmptyInsertCounter>().Add(item);

        int affected = db.Table<EmptyInsertCounter>().Update(item);

        Assert.Equal(1, affected);
    }
}
