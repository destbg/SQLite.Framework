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
}
