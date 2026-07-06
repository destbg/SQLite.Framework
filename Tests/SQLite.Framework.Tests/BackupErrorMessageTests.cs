using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("BackupErrorMessageRow")]
public class BackupErrorMessageRow
{
    [Key]
    public int Id { get; set; }
}

public class BackupErrorMessageTests
{
    [Fact]
    public void BackupOntoFileThatIsNotADatabaseReportsNotADatabaseError()
    {
        string path = Path.Combine(Path.GetTempPath(), $"backup_error_message_{Guid.NewGuid():N}.db3");
        File.WriteAllText(path, "plain text file that is not a sqlite database");
        try
        {
            using TestDatabase source = new();
            source.Table<BackupErrorMessageRow>().Schema.CreateTable();
            source.Table<BackupErrorMessageRow>().Add(new BackupErrorMessageRow { Id = 1 });

            SQLiteException ex = Assert.Throws<SQLiteException>(() => source.BackupTo(path));
            Assert.Equal(SQLiteResult.NonDBFile, ex.Result);
            Assert.NotEqual("not an error", ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void BackupOntoWalFileWithDifferentPageSizeReportsReadOnlyError()
    {
        string path = Path.Combine(Path.GetTempPath(), $"backup_wal_page_{Guid.NewGuid():N}.db3");
        try
        {
            SQLiteOptionsBuilder setupBuilder = new(path);
#if SQLITECIPHER
            setupBuilder.UseEncryptionKey("test-key");
#endif
            using (SQLiteDatabase setup = new(setupBuilder.Build()))
            {
                setup.Execute("PRAGMA page_size = 8192");
                setup.Execute("CREATE TABLE Filler (Id INTEGER)");
                setup.ExecuteScalar<string>("PRAGMA journal_mode = WAL");
                setup.Execute("INSERT INTO Filler VALUES (1)");
            }

            using TestDatabase source = new();
            source.Table<BackupErrorMessageRow>().Schema.CreateTable();
            source.Table<BackupErrorMessageRow>().Add(new BackupErrorMessageRow { Id = 1 });

            SQLiteException ex = Assert.Throws<SQLiteException>(() => source.BackupTo(path));
            Assert.Equal(SQLiteResult.ReadOnly, ex.Result);
            Assert.NotEqual("not an error", ex.Message);
        }
        finally
        {
            File.Delete(path);
            if (File.Exists(path + "-wal"))
            {
                File.Delete(path + "-wal");
            }
            if (File.Exists(path + "-shm"))
            {
                File.Delete(path + "-shm");
            }
        }
    }
}
