using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FkParentLedger")]
public class FkParentLedgerRow
{
    [Key]
    public int Id { get; set; }
}

[Table("FkChildLedger")]
public class FkChildLedgerRow
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }
}

file sealed class FkRenameOrderDb : TestDatabase
{
    public FkRenameOrderDb()
        : base(useFile: true)
    {
    }

    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<FkChildLedgerRow>().ForeignKey<FkParentLedgerRow>(c => c.ParentId);
        builder.Entity<FkParentLedgerRow>().HasColumnName(p => p.Id, "id_num");
    }
}

public class FluentForeignKeyRenameOrderTests
{
    [Fact]
    public void ForeignKeyFollowsALaterParentColumnRename()
    {
        using FkRenameOrderDb db = new();
        db.Schema.CreateTable<FkParentLedgerRow>();
        db.Schema.CreateTable<FkChildLedgerRow>();
        db.Table<FkParentLedgerRow>().Add(new FkParentLedgerRow { Id = 1 });

        Exception? ex = Record.Exception(() => db.Table<FkChildLedgerRow>().Add(new FkChildLedgerRow { Id = 1, ParentId = 1 }));

        Assert.Null(ex);
    }
}
