using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("HookDefaultVariant")]
public class HookDefaultVariantRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public int Count { get; set; }
}

public class ColumnHookDatabaseDefaultVariantTests
{
    [Fact]
    public void AddOrUpdateRerouteAppliesDatabaseDefaultWhenHookSetsNoColumns()
    {
        using ModelTestDatabase db = new(
            m => m.Entity<HookDefaultVariantRow>().Default(r => r.Count, 7),
            o =>
            {
                o.OnAdd<HookDefaultVariantRow>((_, _, _) => true);
                o.OnAction((_, _, action) => action == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : action);
            });
        db.Table<HookDefaultVariantRow>().Schema.CreateTable();

        HookDefaultVariantRow item = new();
        int affected = db.Table<HookDefaultVariantRow>().Add(item);

        Assert.Equal(1, affected);
        Assert.Equal(7, db.ExecuteScalar<long>("SELECT \"Count\" FROM \"HookDefaultVariant\""));
    }

    [Fact]
    public void ReturningAddOrUpdateRerouteAppliesDatabaseDefaultWhenHookSetsNoColumns()
    {
        using ModelTestDatabase db = new(
            m => m.Entity<HookDefaultVariantRow>().Default(r => r.Count, 7),
            o =>
            {
                o.OnAdd<HookDefaultVariantRow>((_, _, _) => true);
                o.OnAction((_, _, action) => action == SQLiteAction.Add ? SQLiteAction.AddOrUpdate : action);
            });
        db.Table<HookDefaultVariantRow>().Schema.CreateTable();

        HookDefaultVariantRow? returned = db.Table<HookDefaultVariantRow>().Returning().Add(new HookDefaultVariantRow());

        Assert.NotNull(returned);
        Assert.Equal(7, returned.Count);
    }

    [Fact]
    public void AddUsesHookColumnValueWhenEntityColumnsAllDefault()
    {
        using ModelTestDatabase db = new(
            m => m.Entity<HookDefaultVariantRow>().Default(r => r.Count, 7),
            o => o.OnAdd<HookDefaultVariantRow>((_, _, columns) =>
            {
                columns["Count"] = 9;
                return true;
            }));
        db.Table<HookDefaultVariantRow>().Schema.CreateTable();

        int affected = db.Table<HookDefaultVariantRow>().Add(new HookDefaultVariantRow());

        Assert.Equal(1, affected);
        Assert.Equal(9, db.ExecuteScalar<long>("SELECT \"Count\" FROM \"HookDefaultVariant\""));
    }

    [Fact]
    public void AddRangeMixesDefaultOnlyAndHookColumnRows()
    {
        int calls = 0;
        using ModelTestDatabase db = new(
            m => m.Entity<HookDefaultVariantRow>().Default(r => r.Count, 7),
            o => o.OnAdd<HookDefaultVariantRow>((_, _, columns) =>
            {
                calls++;
                if (calls == 2)
                {
                    columns["Count"] = 9;
                }

                return true;
            }));
        db.Table<HookDefaultVariantRow>().Schema.CreateTable();

        int affected = db.Table<HookDefaultVariantRow>().AddRange([new HookDefaultVariantRow(), new HookDefaultVariantRow()]);

        Assert.Equal(2, affected);
        Assert.Equal(16, db.ExecuteScalar<long>("SELECT SUM(\"Count\") FROM \"HookDefaultVariant\""));
    }
}
