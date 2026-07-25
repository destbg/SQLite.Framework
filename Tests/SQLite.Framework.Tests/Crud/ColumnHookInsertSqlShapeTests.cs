using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("StampedInsert")]
public class StampedInsertRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class StampedInsertTable : SQLiteTable<StampedInsertRow>
{
    public StampedInsertTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    public int AddInfoCalls { get; private set; }

    protected override (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        AddInfoCalls++;
        return base.GetAddInfo();
    }
}

public class StampedInsertDatabase : TestDatabase
{
    private StampedInsertTable? items;

    public StampedInsertDatabase(Action<SQLiteOptionsBuilder> configure, [CallerMemberName] string? methodName = null)
        : base(configure, methodName)
    {
    }

    public StampedInsertTable Items => items ??= new StampedInsertTable(this, TableMapping(typeof(StampedInsertRow)));
}

public class ColumnHookInsertSqlShapeTests
{
    [Fact]
    public void AnInsertHookOnAnAutoIncrementEntityKeepsTheKeyAssignment()
    {
        using TestDatabase db = new(b => b
            .OnAdd<StampedInsertRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            }));
        db.Table<StampedInsertRow>().Schema.CreateTable();

        db.Table<StampedInsertRow>().Add(new StampedInsertRow { Name = "raw" });

        StampedInsertRow row = db.Table<StampedInsertRow>().Single();
        Assert.Equal("hooked", row.Name);
        Assert.Equal(1, db.ExecuteScalar<long>("SELECT \"Id\" FROM \"StampedInsert\""));
    }

    [Fact]
    public void AnInsertHookCanOverrideTheAutoIncrementKey()
    {
        using TestDatabase db = new(b => b
            .OnAdd<StampedInsertRow>((d, item, columns) =>
            {
                columns["Id"] = 42;
                return true;
            }));
        db.Table<StampedInsertRow>().Schema.CreateTable();

        db.Table<StampedInsertRow>().Add(new StampedInsertRow { Name = "raw" });

        Assert.Equal(42, db.ExecuteScalar<long>("SELECT \"Id\" FROM \"StampedInsert\""));
    }

    [Fact]
    public void AnInsertHookKeepsAnOverriddenAddInfoShape()
    {
        using StampedInsertDatabase db = new(b => b
            .OnAdd<StampedInsertRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            }));
        db.Items.Schema.CreateTable();

        db.Items.Add(new StampedInsertRow { Name = "raw" });

        Assert.Equal("hooked", db.Items.Single().Name);
        Assert.True(db.Items.AddInfoCalls > 0);
    }
}
