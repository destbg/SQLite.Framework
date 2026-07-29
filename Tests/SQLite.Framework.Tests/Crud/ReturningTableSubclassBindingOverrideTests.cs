using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24tCountedRows")]
public class H24tCountedRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H24tCountingTable : SQLiteTable<H24tCountedRow>
{
    public H24tCountingTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    public int Inserts { get; private set; }

    public int Updates { get; private set; }

    public int Deletes { get; private set; }

    protected internal override int InsertItem(TableColumn[] columns, string sql, H24tCountedRow item, bool detectInsertByRowIdChange = false)
    {
        Inserts++;
        return base.InsertItem(columns, sql, item, detectInsertByRowIdChange);
    }

    protected internal override int UpdateItem(TableColumn[] columns, TableColumn[] primaryColumns, string sql, H24tCountedRow item)
    {
        Updates++;
        return base.UpdateItem(columns, primaryColumns, sql, item);
    }

    protected internal override int AddOrRemoveItem(TableColumn[] columns, string sql, H24tCountedRow item)
    {
        Deletes++;
        return base.AddOrRemoveItem(columns, sql, item);
    }
}

public class H24tCountingDatabase : TestDatabase
{
    private H24tCountingTable? rows;

    public H24tCountingDatabase([CallerMemberName] string? methodName = null)
        : base(null, methodName)
    {
    }

    public H24tCountingTable Rows => rows ??= new H24tCountingTable(this, TableMapping(typeof(H24tCountedRow)));
}

public class ReturningTableSubclassBindingOverrideTests
{
    [Fact]
    public void PlainWritesRunThroughTheBindingOverrides()
    {
        using H24tCountingDatabase db = new();
        db.Rows.Schema.CreateTable();

        H24tCountedRow row = new() { Id = 1, Name = "a" };
        db.Rows.Add(row);
        row.Name = "b";
        db.Rows.Update(row);
        db.Rows.Remove(row);

        Assert.Equal(1, db.Rows.Inserts);
        Assert.Equal(1, db.Rows.Updates);
        Assert.Equal(1, db.Rows.Deletes);
    }

    [Fact]
    public void ReturningWritesBuildTheirOwnBindingsBesideTheHelpers()
    {
        using H24tCountingDatabase db = new();
        db.Rows.Schema.CreateTable();

        H24tCountedRow row = new() { Id = 1, Name = "a" };
        H24tCountedRow? added = db.Rows.Returning().Add(row);
        row.Name = "b";
        H24tCountedRow? updated = db.Rows.Returning().Update(row);
        H24tCountedRow? removed = db.Rows.Returning().Remove(row);

        Assert.Equal("a", added!.Name);
        Assert.Equal("b", updated!.Name);
        Assert.Equal("b", removed!.Name);
        Assert.Equal(0, db.Rows.Inserts);
        Assert.Equal(0, db.Rows.Updates);
        Assert.Equal(0, db.Rows.Deletes);
    }
}
