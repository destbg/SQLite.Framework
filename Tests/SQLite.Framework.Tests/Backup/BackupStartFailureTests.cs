using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("BackupStartFailureRow")]
public class BackupStartFailureRow
{
    [Key]
    public int Id { get; set; }
}

public class BackupStartFailureTests
{
    [Fact]
    public void BackupToSameDatabaseThrowsSQLiteException()
    {
        using TestDatabase db = new();
        db.Table<BackupStartFailureRow>().Schema.CreateTable();

        SQLiteException ex = Assert.Throws<SQLiteException>(() => db.BackupTo(db));
        Assert.Contains("distinct", ex.Message);
    }

    [Fact]
    public void BackupFromUnknownSchemaNameThrowsSQLiteException()
    {
        using TestDatabase source = new();
        source.Table<BackupStartFailureRow>().Schema.CreateTable();
        using TestDatabase dest = new();

        SQLiteException ex = Assert.Throws<SQLiteException>(() => source.BackupTo(dest, sourceName: "missing"));
        Assert.Contains("unknown database", ex.Message);
    }

    [Fact]
    public void BackupWhileDestinationHasOpenReaderThrowsSQLiteException()
    {
        using TestDatabase source = new();
        source.Table<BackupStartFailureRow>().Schema.CreateTable();
        source.Table<BackupStartFailureRow>().Add(new BackupStartFailureRow { Id = 1 });

        using TestDatabase dest = new();
        dest.Table<BackupStartFailureRow>().Schema.CreateTable();
        for (int i = 1; i <= 5; i++)
        {
            dest.Table<BackupStartFailureRow>().Add(new BackupStartFailureRow { Id = i });
        }

        using IEnumerator<BackupStartFailureRow> reader = dest.Table<BackupStartFailureRow>().AsEnumerable().GetEnumerator();
        Assert.True(reader.MoveNext());

        SQLiteException ex = Assert.Throws<SQLiteException>(() => source.BackupTo(dest));
        Assert.Contains("in use", ex.Message);
    }
}
