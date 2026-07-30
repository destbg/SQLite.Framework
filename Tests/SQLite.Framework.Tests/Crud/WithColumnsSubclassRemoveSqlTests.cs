using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25iSoftRows")]
public class H25iSoftRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Version { get; set; }

    public int Retired { get; set; }
}

public class H25iSoftTable : SQLiteTable<H25iSoftRow>
{
    public H25iSoftTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    protected internal override (TableColumn[] PrimaryColumns, string Sql) GetRemoveInfo()
    {
        TableColumn[] keys = Table.Columns.Where(c => c.IsPrimaryKey).ToArray();
        return (keys, $"UPDATE \"{Table.TableName}\" SET \"Retired\" = 1 WHERE \"Id\" = @p0");
    }
}

public class H25iSoftDatabase : TestDatabase
{
    private H25iSoftTable? rows;

    public H25iSoftDatabase([CallerMemberName] string? methodName = null)
        : base(null, methodName)
    {
    }

    public H25iSoftTable Rows => rows ??= new H25iSoftTable(this, TableMapping(typeof(H25iSoftRow)));
}

public class H25iAuditTable : SQLiteTable<H25iSoftRow>
{
    public H25iAuditTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    protected override (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        return base.GetAddInfo();
    }

    protected internal override (TableColumn[] Columns, TableColumn[] PrimaryColumns, string Sql) GetUpdateInfo()
    {
        return base.GetUpdateInfo();
    }

    protected internal override (TableColumn[] Columns, string Sql) GetAddOrUpdateInfo(SQLiteConflict conflict)
    {
        return base.GetAddOrUpdateInfo(conflict);
    }

    protected internal override (TableColumn[] Columns, string Sql) GetUpsertInfo(Action<SQLiteUpsertBuilder<H25iSoftRow>> configure)
    {
        return base.GetUpsertInfo(configure);
    }
}

public class H25iAuditDatabase : TestDatabase
{
    private H25iAuditTable? rows;

    public H25iAuditDatabase([CallerMemberName] string? methodName = null)
        : base(null, methodName)
    {
    }

    public H25iAuditTable Rows => rows ??= new H25iAuditTable(this, TableMapping(typeof(H25iSoftRow)));
}

public class WithColumnsSubclassRemoveSqlTests
{
    [Fact]
    public void PlainRemoveRunsTheSubclassRemoveSql()
    {
        using H25iSoftDatabase db = new();
        db.Rows.Schema.CreateTable();
        H25iSoftRow row = new() { Id = 1, Name = "a" };
        db.Rows.Add(row);

        db.Rows.Remove(row);

        Assert.Equal(1, db.ExecuteScalar<int>("SELECT COUNT(*) FROM \"H25iSoftRows\""));
        Assert.Equal(1, db.ExecuteScalar<int>("SELECT \"Retired\" FROM \"H25iSoftRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void RemoveThroughWithColumnsRunsTheSubclassRemoveSql()
    {
        using H25iSoftDatabase db = new();
        db.Rows.Schema.CreateTable();
        H25iSoftRow row = new() { Id = 1, Name = "a" };
        db.Rows.Add(row);

        db.Rows.WithColumns(c => c.Set(x => x.Version, 7)).Remove(row);

        Assert.Equal(1, db.ExecuteScalar<int>("SELECT COUNT(*) FROM \"H25iSoftRows\""));
        Assert.Equal(1, db.ExecuteScalar<int>("SELECT \"Retired\" FROM \"H25iSoftRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void AddThroughWithColumnsRunsTheSubclassAddSql()
    {
        using H25iAuditDatabase db = new();
        db.Rows.Schema.CreateTable();

        db.Rows.WithColumns(c => c.Set(x => x.Version, 7)).Add(new H25iSoftRow { Id = 1, Name = "a" });

        Assert.Equal(1, db.ExecuteScalar<int>("SELECT COUNT(*) FROM \"H25iSoftRows\""));
        Assert.Equal(0, db.ExecuteScalar<int>("SELECT \"Version\" FROM \"H25iSoftRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void UpdateThroughWithColumnsRunsTheSubclassUpdateSql()
    {
        using H25iAuditDatabase db = new();
        db.Rows.Schema.CreateTable();
        H25iSoftRow row = new() { Id = 1, Name = "a" };
        db.Rows.Add(row);

        row.Name = "b";
        db.Rows.WithColumns(c => c.Set(x => x.Version, 7)).Update(row);

        Assert.Equal("b", db.ExecuteScalar<string>("SELECT \"Name\" FROM \"H25iSoftRows\" WHERE \"Id\" = 1"));
        Assert.Equal(0, db.ExecuteScalar<int>("SELECT \"Version\" FROM \"H25iSoftRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void AddOrUpdateThroughWithColumnsRunsTheSubclassAddOrUpdateSql()
    {
        using H25iAuditDatabase db = new();
        db.Rows.Schema.CreateTable();
        db.Rows.Add(new H25iSoftRow { Id = 1, Name = "a" });

        db.Rows.WithColumns(c => c.Set(x => x.Version, 7)).AddOrUpdate(new H25iSoftRow { Id = 1, Name = "b" });

        Assert.Equal("b", db.ExecuteScalar<string>("SELECT \"Name\" FROM \"H25iSoftRows\" WHERE \"Id\" = 1"));
        Assert.Equal(0, db.ExecuteScalar<int>("SELECT \"Version\" FROM \"H25iSoftRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void UpsertThroughWithColumnsRunsTheSubclassUpsertSql()
    {
        using H25iAuditDatabase db = new();
        db.Rows.Schema.CreateTable();
        db.Rows.Add(new H25iSoftRow { Id = 1, Name = "a" });

        db.Rows.WithColumns(c => c.Set(x => x.Version, 7))
            .Upsert(new H25iSoftRow { Id = 1, Name = "b" }, c => c.OnConflict(x => x.Id).DoUpdate(x => x.Name));

        Assert.Equal("b", db.ExecuteScalar<string>("SELECT \"Name\" FROM \"H25iSoftRows\" WHERE \"Id\" = 1"));
        Assert.Equal(0, db.ExecuteScalar<int>("SELECT \"Version\" FROM \"H25iSoftRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void RemoveRangeThroughWithColumnsRunsTheSubclassRemoveSql()
    {
        using H25iSoftDatabase db = new();
        db.Rows.Schema.CreateTable();
        List<H25iSoftRow> rows =
        [
            new H25iSoftRow { Id = 1, Name = "a" },
            new H25iSoftRow { Id = 2, Name = "b" }
        ];
        db.Rows.AddRange(rows);

        db.Rows.WithColumns(c => c.Set(x => x.Version, 7)).RemoveRange(rows);

        Assert.Equal(2, db.ExecuteScalar<int>("SELECT COUNT(*) FROM \"H25iSoftRows\""));
        Assert.Equal(2L, db.ExecuteScalar<long>("SELECT SUM(\"Retired\") FROM \"H25iSoftRows\""));
    }
}
