using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24tStampedRows")]
public class H24tStampedRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Version { get; set; }
}

public class H24tStampedTable : SQLiteTable<H24tStampedRow>
{
    public H24tStampedTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    protected internal override string WrapParam(string placeholder, TableColumn column)
    {
        return column.Name == "Name" ? $"upper({placeholder})" : base.WrapParam(placeholder, column);
    }
}

public class H24tStampedDatabase : TestDatabase
{
    private H24tStampedTable? rows;

    public H24tStampedDatabase([CallerMemberName] string? methodName = null)
        : base(null, methodName)
    {
    }

    public H24tStampedTable Rows => rows ??= new H24tStampedTable(this, TableMapping(typeof(H24tStampedRow)));
}

public class WithColumnsTableSubclassOverrideTests
{
    [Fact]
    public void WithColumnsKeepsTheParameterWrapOverrideOnAdd()
    {
        using H24tStampedDatabase db = new();
        db.Rows.Schema.CreateTable();

        db.Rows.Add(new H24tStampedRow { Id = 1, Name = "plain" });
        db.Rows.WithColumns(c => c.Set(x => x.Version, 9)).Add(new H24tStampedRow { Id = 2, Name = "extra" });

        List<string> expected = ["PLAIN", "EXTRA"];
        List<string> actual = db.Rows.OrderBy(r => r.Id).Select(r => r.Name).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WithColumnsKeepsTheParameterWrapOverrideOnUpdate()
    {
        using H24tStampedDatabase db = new();
        db.Rows.Schema.CreateTable();
        H24tStampedRow row = new() { Id = 1, Name = "one" };
        db.Rows.Add(row);

        row.Name = "two";
        db.Rows.WithColumns(c => c.Set(x => x.Version, 4)).Update(row);

        H24tStampedRow stored = db.Rows.Single();

        Assert.Equal("TWO", stored.Name);
        Assert.Equal(4, stored.Version);
    }

    [Fact]
    public void WithColumnsRemoveDeletesTheRow()
    {
        using H24tStampedDatabase db = new();
        db.Rows.Schema.CreateTable();
        H24tStampedRow row = new() { Id = 1, Name = "plain" };
        db.Rows.Add(row);

        int removed = db.Rows.WithColumns(c => c.Set(x => x.Version, 1)).Remove(row);

        Assert.Equal(1, removed);
        Assert.Empty(db.Rows.ToList());
    }
}
