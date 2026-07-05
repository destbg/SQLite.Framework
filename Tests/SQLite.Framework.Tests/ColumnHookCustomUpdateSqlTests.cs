using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GuardedUpdate")]
public class GuardedUpdateRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class GuardedUpdateTable : SQLiteTable<GuardedUpdateRow>
{
    public GuardedUpdateTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    protected internal override (TableColumn[] Columns, TableColumn[] PrimaryColumns, string Sql) GetUpdateInfo()
    {
        (TableColumn[] columns, TableColumn[] primaryColumns, string sql) = base.GetUpdateInfo();
        return (columns, primaryColumns, sql + " AND \"Name\" = 'locked'");
    }
}

public class GuardedUpdateDatabase : TestDatabase
{
    private GuardedUpdateTable? items;

    public GuardedUpdateDatabase(Action<SQLiteOptionsBuilder> configure, [CallerMemberName] string? methodName = null)
        : base(configure, methodName)
    {
    }

    public GuardedUpdateTable Items => items ??= new GuardedUpdateTable(this, TableMapping(typeof(GuardedUpdateRow)));
}

public class ColumnHookCustomUpdateSqlTests
{
    [Fact]
    public void AColumnHookKeepsTheCustomUpdateSqlShape()
    {
        using GuardedUpdateDatabase db = new(b => b
            .OnUpdate<GuardedUpdateRow>((d, item, columns) =>
            {
                columns["Name"] = "hooked";
                return true;
            }));
        db.Items.Schema.CreateTable();
        db.Items.Add(new GuardedUpdateRow { Id = 1, Name = "seed" });

        db.Items.Update(new GuardedUpdateRow { Id = 1, Name = "raw" });

        Assert.Equal("seed", db.Items.Single().Name);
    }
}
