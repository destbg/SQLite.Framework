using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("BackupTx")]
public class BackupTxRow
{
    [Key]
    public int Id { get; set; }
}

public class BackupDuringTransactionTests
{
    [Fact]
    public void BackupDuringOpenSourceTransactionThrows()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<BackupTxRow>().Schema.CreateTable();
        string dest = db.Options.DatabasePath + ".bak";

        using SQLiteTransaction tx = db.BeginTransaction();
        db.Table<BackupTxRow>().Add(new BackupTxRow { Id = 1 });

        Assert.Throws<InvalidOperationException>(() => db.BackupTo(dest));

        tx.Commit();
        db.BackupTo(dest);
        Assert.True(File.Exists(dest));
    }

    [Fact]
    public void BackupDuringOpenDestinationTransactionThrows()
    {
        using TestDatabase source = new(useFile: true);
        using TestDatabase dest = new(useFile: true);
        source.Table<BackupTxRow>().Schema.CreateTable();
        dest.Table<BackupTxRow>().Schema.CreateTable();

        using SQLiteTransaction tx = dest.BeginTransaction();
        dest.Table<BackupTxRow>().Add(new BackupTxRow { Id = 1 });

        Assert.Throws<InvalidOperationException>(() => source.BackupTo(dest));

        tx.Commit();
    }
}
