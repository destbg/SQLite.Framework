using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26fGuardedRows")]
public class H26fGuardedRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Locked { get; set; }

    public string? Note { get; set; }
}

public class H26fGuardedTable : SQLiteTable<H26fGuardedRow>
{
    public H26fGuardedTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    protected override (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        TableColumn[] columns = Table.Columns.Where(c => c.Name != "Note").ToArray();
        string names = string.Join(", ", columns.Select(c => $"\"{c.Name}\""));
        string values = string.Join(", ", columns.Select((_, i) => $"@p{i}"));
        return (columns, $"INSERT OR IGNORE INTO \"H26fGuardedRows\" ({names}) VALUES ({values})");
    }

    protected internal override (TableColumn[] Columns, TableColumn[] PrimaryColumns, string Sql) GetUpdateInfo()
    {
        TableColumn[] columns = Table.Columns.Where(c => !c.IsPrimaryKey && c.Name != "Note").ToArray();
        TableColumn[] keys = Table.Columns.Where(c => c.IsPrimaryKey).ToArray();
        string sets = string.Join(", ", columns.Select((c, i) => $"\"{c.Name}\" = @p{i}"));
        return (columns, keys, $"UPDATE \"H26fGuardedRows\" SET {sets} WHERE \"Id\" = @p{columns.Length} AND \"Locked\" = 0");
    }
}

public class H26fGuardedDatabase : TestDatabase
{
    private H26fGuardedTable? rows;

    public H26fGuardedDatabase(Action<SQLiteOptionsBuilder>? configure, [CallerMemberName] string? methodName = null)
        : base(configure, methodName)
    {
    }

    public H26fGuardedTable Rows => rows ??= new H26fGuardedTable(this, TableMapping(typeof(H26fGuardedRow)));
}

public class SubclassWriteSqlWithColumnHookTests
{
    [Fact]
    public void AColumnHookKeepsTheSubclassInsertConflictClause()
    {
        using H26fGuardedDatabase db = new(b => b.OnAdd<H26fGuardedRow>((_, _, columns) =>
        {
            columns["Note"] = "hooked";
            return true;
        }));
        db.Rows.Schema.CreateTable();
        db.Rows.Add(new H26fGuardedRow { Id = 1, Name = "first" });

        int again = db.Rows.Add(new H26fGuardedRow { Id = 1, Name = "second" });

        Assert.Equal(0, again);
        Assert.Equal("first", db.ExecuteScalar<string>("SELECT \"Name\" FROM \"H26fGuardedRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void AColumnHookKeepsTheSubclassUpdateGuard()
    {
        using H26fGuardedDatabase db = new(b => b.OnUpdate<H26fGuardedRow>((_, _, columns) =>
        {
            columns["Note"] = "hooked";
            return true;
        }));
        db.Rows.Schema.CreateTable();
        H26fGuardedRow row = new() { Id = 1, Name = "original", Locked = 1 };
        db.Rows.Add(row);

        row.Name = "changed";
        int affected = db.Rows.Update(row);

        Assert.Equal(0, affected);
        Assert.Equal("original", db.ExecuteScalar<string>("SELECT \"Name\" FROM \"H26fGuardedRows\" WHERE \"Id\" = 1"));
    }
}
