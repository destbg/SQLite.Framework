using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RerouteHookRow")]
public class RerouteHookRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ActionHookRerouteKeepsColumnHookValuesTests
{
    [Fact]
    public void ColumnHookValueAppliesOnPlainAdd()
    {
        using TestDatabase db = new(b => b
            .OnAdd<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            }));
        db.Table<RerouteHookRow>().Schema.CreateTable();

        db.Table<RerouteHookRow>().Add(new RerouteHookRow { Name = "raw" });

        Assert.Equal("hooked", db.Table<RerouteHookRow>().Single().Name);
    }

    [Fact]
    public void ColumnHookValueSurvivesAddReroute()
    {
        using TestDatabase db = new(b => b
            .OnAdd<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            })
            .OnAction((d, entity, action) => action == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();

        db.Table<RerouteHookRow>().Add(new RerouteHookRow { Name = "raw" });

        Assert.Equal("hooked", db.Table<RerouteHookRow>().Single().Name);
    }

    [Fact]
    public void ColumnHookValueSurvivesUpdateReroute()
    {
        using TestDatabase db = new(b => b
            .OnUpdate<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            })
            .OnAction((d, entity, action) => action == SQLiteAction.Update ? SQLiteAction.AddOrUpdate : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();
        db.Table<RerouteHookRow>().Add(new RerouteHookRow { Name = "seed" });

        db.Table<RerouteHookRow>().Update(new RerouteHookRow { Id = 1, Name = "raw" });

        Assert.Equal("hooked", db.Table<RerouteHookRow>().Single().Name);
    }

    [Fact]
    public void ColumnHookValueSurvivesAddRangeReroute()
    {
        using TestDatabase db = new(b => b
            .OnAdd<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            })
            .OnAction((d, entity, action) => action == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();

        db.Table<RerouteHookRow>().AddRange([new RerouteHookRow { Name = "raw" }]);

        Assert.Equal("hooked", db.Table<RerouteHookRow>().Single().Name);
    }

    [Fact]
    public void ColumnHookValueSurvivesReturningAddReroute()
    {
        using TestDatabase db = new(b => b
            .OnAdd<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            })
            .OnAction((d, entity, action) => action == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();

        RerouteHookRow? returned = db.Table<RerouteHookRow>().Returning().Add(new RerouteHookRow { Name = "raw" });

        Assert.NotNull(returned);
        Assert.Equal("hooked", db.Table<RerouteHookRow>().Single().Name);
    }

    [Fact]
    public void EmptyColumnHookRerouteFallsBackToPlainDispatch()
    {
        using TestDatabase db = new(b => b
            .OnAdd<RerouteHookRow>((d, item, columns) => true)
            .OnAction((d, entity, action) => action == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();

        db.Table<RerouteHookRow>().Add(new RerouteHookRow { Name = "raw" });

        Assert.Equal("raw", db.Table<RerouteHookRow>().Single().Name);
    }

    [Fact]
    public void ColumnHookValueSurvivesUpdateToAddReroute()
    {
        using TestDatabase db = new(b => b
            .OnUpdate<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            })
            .OnAction((d, entity, action) => action == SQLiteAction.Update ? SQLiteAction.Add : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();

        db.Table<RerouteHookRow>().Update(new RerouteHookRow { Id = 5, Name = "raw" });

        Assert.Equal("hooked", db.Table<RerouteHookRow>().Single().Name);
    }

    [Fact]
    public void ColumnHookValueSurvivesAddToUpdateReroute()
    {
        using TestDatabase db = new(b => b
            .OnAdd<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            })
            .OnAction((d, entity, action) => action == SQLiteAction.Add ? SQLiteAction.Update : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();
        db.Execute("INSERT INTO \"RerouteHookRow\" (\"Id\", \"Name\") VALUES (1, 'seed')");

        db.Table<RerouteHookRow>().Add(new RerouteHookRow { Id = 1, Name = "raw" });

        Assert.Equal("hooked", db.Table<RerouteHookRow>().Single().Name);
    }

    [Fact]
    public void ColumnHookRerouteToRemoveDeletesTheRow()
    {
        using TestDatabase db = new(b => b
            .OnUpdate<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            })
            .OnAction((d, entity, action) => action == SQLiteAction.Update ? SQLiteAction.Remove : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();
        db.Execute("INSERT INTO \"RerouteHookRow\" (\"Id\", \"Name\") VALUES (1, 'seed')");

        db.Table<RerouteHookRow>().Update(new RerouteHookRow { Id = 1, Name = "raw" });

        Assert.Empty(db.Table<RerouteHookRow>().ToList());
    }

    [Fact]
    public void ColumnHookRerouteToSkipWritesNothing()
    {
        using TestDatabase db = new(b => b
            .OnAdd<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            })
            .OnAction((d, entity, action) => action == SQLiteAction.Add ? SQLiteAction.Skip : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();

        db.Table<RerouteHookRow>().Add(new RerouteHookRow { Name = "raw" });

        Assert.Empty(db.Table<RerouteHookRow>().ToList());
    }

    [Fact]
    public void ColumnHookValueSurvivesReturningUpdateReroute()
    {
        using TestDatabase db = new(b => b
            .OnAdd<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            })
            .OnAction((d, entity, action) => action == SQLiteAction.Add ? SQLiteAction.Update : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();
        db.Execute("INSERT INTO \"RerouteHookRow\" (\"Id\", \"Name\") VALUES (1, 'seed')");

        RerouteHookRow? returned = db.Table<RerouteHookRow>().Returning().Add(new RerouteHookRow { Id = 1, Name = "raw" });

        Assert.NotNull(returned);
        Assert.Equal("hooked", db.Table<RerouteHookRow>().Single().Name);
    }

    [Fact]
    public void ColumnHookValueSurvivesReturningUpdateToAddReroute()
    {
        using TestDatabase db = new(b => b
            .OnUpdate<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            })
            .OnAction((d, entity, action) => action == SQLiteAction.Update ? SQLiteAction.Add : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();

        RerouteHookRow? returned = db.Table<RerouteHookRow>().Returning().Update(new RerouteHookRow { Id = 5, Name = "raw" });

        Assert.NotNull(returned);
        Assert.Equal("hooked", db.Table<RerouteHookRow>().Single().Name);
    }

    [Fact]
    public void EmptyColumnHookRerouteOnReturningAddFallsBackToPlainDispatch()
    {
        using TestDatabase db = new(b => b
            .OnAdd<RerouteHookRow>((d, item, columns) => true)
            .OnAction((d, entity, action) => action == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();

        RerouteHookRow? returned = db.Table<RerouteHookRow>().Returning().Add(new RerouteHookRow { Name = "raw" });

        Assert.NotNull(returned);
        Assert.Equal("raw", db.Table<RerouteHookRow>().Single().Name);
    }

    [Fact]
    public void ColumnHookRerouteToSkipReturnsNothingOnReturningAdd()
    {
        using TestDatabase db = new(b => b
            .OnAdd<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            })
            .OnAction((d, entity, action) => action == SQLiteAction.Add ? SQLiteAction.Skip : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();

        RerouteHookRow? returned = db.Table<RerouteHookRow>().Returning().Add(new RerouteHookRow { Name = "raw" });

        Assert.Null(returned);
        Assert.Empty(db.Table<RerouteHookRow>().ToList());
    }

    [Fact]
    public void ColumnHookRerouteToRemoveOnReturningUpdateDeletesTheRow()
    {
        using TestDatabase db = new(b => b
            .OnUpdate<RerouteHookRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            })
            .OnAction((d, entity, action) => action == SQLiteAction.Update ? SQLiteAction.Remove : action));
        db.Table<RerouteHookRow>().Schema.CreateTable();
        db.Execute("INSERT INTO \"RerouteHookRow\" (\"Id\", \"Name\") VALUES (1, 'seed')");

        RerouteHookRow? returned = db.Table<RerouteHookRow>().Returning().Update(new RerouteHookRow { Id = 1, Name = "raw" });

        Assert.NotNull(returned);
        Assert.Empty(db.Table<RerouteHookRow>().ToList());
    }
}
