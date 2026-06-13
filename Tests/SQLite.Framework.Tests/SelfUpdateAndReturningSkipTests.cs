using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class OnlyAutoKeyRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
}

internal sealed class ReturningSkipRow
{
    [Key]
    public int Id { get; set; }

    public required string Text { get; set; }
}

public class SelfUpdateAndReturningSkipTests
{
    [Fact]
    public void UpdateEntityWithOnlyAutoKeyColumnAffectsRow()
    {
        using TestDatabase db = new();
        db.Table<OnlyAutoKeyRow>().Schema.CreateTable();
        db.Table<OnlyAutoKeyRow>().Add(new OnlyAutoKeyRow());

        int affected = db.Table<OnlyAutoKeyRow>().Update(new OnlyAutoKeyRow { Id = 1 });

        Assert.Equal(1, affected);
    }

    [Fact]
    public void ReturningUpdateReturnsDefaultWhenActionHookSkips()
    {
        using TestDatabase db = new(b => b.OnAction((_, _, action) =>
            action == SQLiteAction.Update ? SQLiteAction.Skip : action));
        db.Table<ReturningSkipRow>().Schema.CreateTable();
        db.Table<ReturningSkipRow>().Add(new ReturningSkipRow { Id = 1, Text = "a" });

        ReturningSkipRow? result = db.Table<ReturningSkipRow>()
            .Returning()
            .Update(new ReturningSkipRow { Id = 1, Text = "b" });

        Assert.Null(result);
        Assert.Equal("a", db.Table<ReturningSkipRow>().Single().Text);
    }
}
