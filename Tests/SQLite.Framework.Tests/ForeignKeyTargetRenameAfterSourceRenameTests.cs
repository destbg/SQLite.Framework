using System;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class FkSourceThenParentRenameDb : TestDatabase
{
    public FkSourceThenParentRenameDb()
        : base(useFile: true)
    {
    }

    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<FkChildLedgerRow>().ForeignKey<FkParentLedgerRow>(c => c.ParentId);
        builder.Entity<FkChildLedgerRow>().HasColumnName(c => c.ParentId, "fk_parent");
        builder.Entity<FkParentLedgerRow>().HasColumnName(p => p.Id, "renamed_id");
    }
}

public class ForeignKeyTargetRenameAfterSourceRenameTests
{
    [Fact]
    public void ForeignKeyFollowsParentRenameDeclaredAfterSourceRename()
    {
        using FkSourceThenParentRenameDb db = new();
        db.Schema.CreateTable<FkParentLedgerRow>();
        db.Schema.CreateTable<FkChildLedgerRow>();
        db.Table<FkParentLedgerRow>().Add(new FkParentLedgerRow { Id = 1 });

        Exception? ex = Record.Exception(() => db.Table<FkChildLedgerRow>().Add(new FkChildLedgerRow { Id = 1, ParentId = 1 }));

        Assert.Null(ex);
    }
}
