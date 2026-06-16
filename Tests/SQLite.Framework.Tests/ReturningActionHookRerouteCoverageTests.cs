using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class RerouteCoverageRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public bool IsDeleted { get; set; }
}

public class ReturningActionHookRerouteCoverageTests
{
    [Fact]
    public void ReturningAddRerouteToAddOrUpdateWritesRow()
    {
        using TestDatabase db = new(b => b.OnAction((_, _, a) => a == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : a));
        db.Table<RerouteCoverageRow>().Schema.CreateTable();

        RerouteCoverageRow? returned = db.Table<RerouteCoverageRow>().Returning().Add(new RerouteCoverageRow { Id = 1, Name = "x" });

        Assert.NotNull(returned);
        Assert.Equal("x", returned!.Name);
        Assert.Equal("x", db.Table<RerouteCoverageRow>().Single().Name);
    }

    [Fact]
    public void ReturningAddRerouteToUnknownActionThrows()
    {
        using TestDatabase db = new(b => b.OnAction((_, _, _) => (SQLiteAction)999));
        db.Table<RerouteCoverageRow>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() =>
            db.Table<RerouteCoverageRow>().Returning().Add(new RerouteCoverageRow { Id = 1, Name = "x" }));
    }

    [Fact]
    public void ReturningUpsertRerouteToRemoveDeletesRow()
    {
        using TestDatabase db = new(b => b.OnAction((_, _, a) => a == SQLiteAction.AddOrUpdate ? SQLiteAction.Remove : a));
        db.Table<RerouteCoverageRow>().Schema.CreateTable();
        db.Table<RerouteCoverageRow>().Add(new RerouteCoverageRow { Id = 1, Name = "x" });

        db.Table<RerouteCoverageRow>().Returning().Upsert(
            new RerouteCoverageRow { Id = 1, Name = "y" },
            c => c.OnConflict(x => x.Id).DoUpdate(x => x.Name));

        Assert.Empty(db.Table<RerouteCoverageRow>().ToList());
    }

    [Fact]
    public void ReturningRemoveRangeRerouteToUpdateKeepsRows()
    {
        using TestDatabase db = new(b => b.OnAction((_, e, a) =>
        {
            if (a == SQLiteAction.Remove && e is RerouteCoverageRow row)
            {
                row.IsDeleted = true;
                return SQLiteAction.Update;
            }

            return a;
        }));
        db.Table<RerouteCoverageRow>().Schema.CreateTable();
        db.Table<RerouteCoverageRow>().Add(new RerouteCoverageRow { Id = 1, Name = "x" });

        RerouteCoverageRow row = db.Table<RerouteCoverageRow>().Single();
        db.Table<RerouteCoverageRow>().Returning().RemoveRange([row]);

        List<RerouteCoverageRow> remaining = db.Table<RerouteCoverageRow>().ToList();

        Assert.Single(remaining);
        Assert.True(remaining[0].IsDeleted);
    }
}
