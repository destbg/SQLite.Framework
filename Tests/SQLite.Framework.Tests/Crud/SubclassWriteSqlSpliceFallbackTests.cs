using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SpliceFallbackRows")]
public class SpliceFallbackRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Note { get; set; }

    public string? Audit { get; set; }
}

public class SpliceSelectInsertTable : SQLiteTable<SpliceFallbackRow>
{
    public SpliceSelectInsertTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    protected override (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        TableColumn[] columns = Table.Columns.Where(c => c.Name is "Id" or "Name").ToArray();
        return (columns, "INSERT INTO \"SpliceFallbackRows\" (\"Id\", \"Name\") SELECT @p0, @p1");
    }
}

public class SpliceConflictSuffixInsertTable : SQLiteTable<SpliceFallbackRow>
{
    public SpliceConflictSuffixInsertTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    protected override (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        TableColumn[] columns = Table.Columns.Where(c => c.Name is "Id" or "Name").ToArray();
        return (columns, "INSERT INTO \"SpliceFallbackRows\" (\"Id\", \"Name\") VALUES (@p0, @p1) ON CONFLICT DO NOTHING");
    }
}

public class SpliceBareValuesInsertTable : SQLiteTable<SpliceFallbackRow>
{
    public SpliceBareValuesInsertTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    protected override (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        TableColumn[] columns = Table.Columns.Where(c => c.Name is "Id" or "Name" or "Note").ToArray();
        return (columns, "INSERT INTO \"SpliceFallbackRows\" VALUES (@p0, @p1, @p2, NULL)");
    }
}

public class SpliceNoWhereUpdateTable : SQLiteTable<SpliceFallbackRow>
{
    public SpliceNoWhereUpdateTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    protected internal override (TableColumn[] Columns, TableColumn[] PrimaryColumns, string Sql) GetUpdateInfo()
    {
        TableColumn[] columns = Table.Columns.Where(c => c.Name == "Name").ToArray();
        TableColumn[] keys = Table.Columns.Where(c => c.IsPrimaryKey).ToArray();
        return (columns, keys, "UPDATE \"SpliceFallbackRows\" SET \"Name\" = @p0");
    }
}

public class SpliceHappyTable : SQLiteTable<SpliceFallbackRow>
{
    public SpliceHappyTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    protected override (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        TableColumn[] columns = Table.Columns.Where(c => c.Name is "Id" or "Name").ToArray();
        return (columns, "INSERT INTO \"SpliceFallbackRows\" (\"Id\", \"Name\") VALUES (@p0, @p1)");
    }

    protected internal override (TableColumn[] Columns, TableColumn[] PrimaryColumns, string Sql) GetUpdateInfo()
    {
        TableColumn[] columns = Table.Columns.Where(c => c.Name == "Name").ToArray();
        TableColumn[] keys = Table.Columns.Where(c => c.IsPrimaryKey).ToArray();
        return (columns, keys, "UPDATE \"SpliceFallbackRows\" SET \"Name\" = @p0 WHERE \"Id\" = @p1");
    }
}

public class SpliceFallbackDatabase : TestDatabase
{
    public SpliceFallbackDatabase(Action<SQLiteOptionsBuilder>? configure, [CallerMemberName] string? methodName = null)
        : base(configure, methodName)
    {
        Table<SpliceFallbackRow>().Schema.CreateTable();
    }
}

public class SubclassWriteSqlSpliceFallbackTests
{
    [Fact]
    public void ACustomInsertWithoutAValuesClauseFallsBackToAGeneratedInsert()
    {
        using SpliceFallbackDatabase db = new(b => b.OnAdd<SpliceFallbackRow>((_, _, columns) =>
        {
            columns["Note"] = "hooked";
            return true;
        }));
        SpliceSelectInsertTable rows = new(db, db.TableMapping(typeof(SpliceFallbackRow)));

        rows.Add(new SpliceFallbackRow { Id = 1, Name = "first" });

        Assert.Equal("hooked", db.ExecuteScalar<string>("SELECT \"Note\" FROM \"SpliceFallbackRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void ACustomInsertWithAConflictSuffixFallsBackToAGeneratedInsert()
    {
        using SpliceFallbackDatabase db = new(b => b.OnAdd<SpliceFallbackRow>((_, _, columns) =>
        {
            columns["Note"] = "hooked";
            return true;
        }));
        SpliceConflictSuffixInsertTable rows = new(db, db.TableMapping(typeof(SpliceFallbackRow)));

        rows.Add(new SpliceFallbackRow { Id = 1, Name = "first" });

        Assert.Equal("hooked", db.ExecuteScalar<string>("SELECT \"Note\" FROM \"SpliceFallbackRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void ACustomInsertWithoutAColumnListFallsBackToAGeneratedInsert()
    {
        using SpliceFallbackDatabase db = new(b => b.OnAdd<SpliceFallbackRow>((_, _, columns) =>
        {
            columns["Audit"] = "hooked-audit";
            return true;
        }));
        SpliceBareValuesInsertTable rows = new(db, db.TableMapping(typeof(SpliceFallbackRow)));

        rows.Add(new SpliceFallbackRow { Id = 1, Name = "first", Note = "note" });

        Assert.Equal("hooked-audit", db.ExecuteScalar<string>("SELECT \"Audit\" FROM \"SpliceFallbackRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void ACustomUpdateWithoutAWhereClauseFallsBackToAGeneratedUpdate()
    {
        using SpliceFallbackDatabase db = new(b => b.OnUpdate<SpliceFallbackRow>((_, _, columns) =>
        {
            columns["Note"] = "hooked";
            return true;
        }));
        SpliceNoWhereUpdateTable rows = new(db, db.TableMapping(typeof(SpliceFallbackRow)));
        SpliceFallbackRow row = new() { Id = 1, Name = "first" };
        rows.Add(row);

        row.Name = "changed";
        rows.Update(row);

        Assert.Equal("changed", db.ExecuteScalar<string>("SELECT \"Name\" FROM \"SpliceFallbackRows\" WHERE \"Id\" = 1"));
        Assert.Equal("hooked", db.ExecuteScalar<string>("SELECT \"Note\" FROM \"SpliceFallbackRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void ASplicedCustomInsertMergesHookColumnsAndDeclaredColumns()
    {
        using SpliceFallbackDatabase db = new(b => b.OnAdd<SpliceFallbackRow>((_, _, columns) =>
        {
            columns["Note"] = null;
            columns["Name"] = "hooked-name";
            return true;
        }));
        SpliceHappyTable rows = new(db, db.TableMapping(typeof(SpliceFallbackRow)));

        rows.WithColumns(c => c.Set(x => x.Audit, "wc-audit").Set(x => x.Note, "wc-note"))
            .Add(new SpliceFallbackRow { Id = 1, Name = "first" });

        Assert.Equal("hooked-name", db.ExecuteScalar<string>("SELECT \"Name\" FROM \"SpliceFallbackRows\" WHERE \"Id\" = 1"));
        Assert.Null(db.ExecuteScalar<string>("SELECT \"Note\" FROM \"SpliceFallbackRows\" WHERE \"Id\" = 1"));
        Assert.Equal("wc-audit", db.ExecuteScalar<string>("SELECT \"Audit\" FROM \"SpliceFallbackRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void ASplicedCustomUpdateMergesHookColumnsAndDeclaredColumns()
    {
        using SpliceFallbackDatabase db = new(b => b.OnUpdate<SpliceFallbackRow>((_, _, columns) =>
        {
            columns["Note"] = null;
            columns["Name"] = "hooked-name";
            return true;
        }));
        SpliceHappyTable rows = new(db, db.TableMapping(typeof(SpliceFallbackRow)));
        SpliceFallbackRow row = new() { Id = 1, Name = "first", Note = "before" };
        rows.Add(row);

        row.Name = "changed";
        rows.WithColumns(c => c.Set(x => x.Audit, "wc-audit").Set(x => x.Note, "wc-note"))
            .Update(row);

        Assert.Equal("hooked-name", db.ExecuteScalar<string>("SELECT \"Name\" FROM \"SpliceFallbackRows\" WHERE \"Id\" = 1"));
        Assert.Null(db.ExecuteScalar<string>("SELECT \"Note\" FROM \"SpliceFallbackRows\" WHERE \"Id\" = 1"));
        Assert.Equal("wc-audit", db.ExecuteScalar<string>("SELECT \"Audit\" FROM \"SpliceFallbackRows\" WHERE \"Id\" = 1"));
    }
}
