using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BackupOntoSameFileTests
{
    [Fact]
    public void BackupToSecondConnectionOnSameFileThrows()
    {
        using TestDatabase main = new(useFile: true);
        main.Execute("CREATE TABLE \"H20AsyncSelfCopy\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");

        SQLiteOptionsBuilder builder = new(main.Options.DatabasePath);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        using SQLiteDatabase second = new(builder.Build());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => main.BackupTo(second));
        Assert.Equal("BackupTo cannot copy a database onto itself. The backup would hold a read lock on the source file that the destination write can never pass, so the copy could never finish. Back up to a different file.", exception.Message);
    }

    [Fact]
    public async Task BackupToAsyncSecondConnectionOnSameFileThrows()
    {
        using TestDatabase main = new(useFile: true);
        main.Execute("CREATE TABLE \"H20AsyncSelfCopy\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");

        SQLiteOptionsBuilder builder = new(main.Options.DatabasePath);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        using SQLiteDatabase second = new(builder.Build());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => main.BackupToAsync(second));
        Assert.Equal("BackupTo cannot copy a database onto itself. The backup would hold a read lock on the source file that the destination write can never pass, so the copy could never finish. Back up to a different file.", exception.Message);
    }

    [Fact]
    public void BackupToPathOfSameFileThrows()
    {
        using TestDatabase main = new(useFile: true);
        main.Execute("CREATE TABLE \"H20AsyncSelfCopy\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => main.BackupTo(main.Options.DatabasePath));
        Assert.Equal("BackupTo cannot copy a database onto itself. The backup would hold a read lock on the source file that the destination write can never pass, so the copy could never finish. Back up to a different file.", exception.Message);
    }

    [Fact]
    public void BackupToSecondConnectionOnDifferentFileCopiesRows()
    {
        List<int> values = Enumerable.Range(1, 50).ToList();
        using TestDatabase main = new(useFile: true);
        main.Execute("CREATE TABLE \"H20AsyncSelfCopy\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");
        foreach (int value in values)
        {
            main.Execute($"INSERT INTO \"H20AsyncSelfCopy\" (\"Id\", \"Value\") VALUES ({value}, {value})");
        }

        string path = Path.Combine(Path.GetTempPath(), $"backup_other_{Guid.NewGuid():N}.db3");
        try
        {
            SQLiteOptionsBuilder builder = new(path);
#if SQLITECIPHER
            builder.UseEncryptionKey("test-key");
#endif
            using SQLiteDatabase second = new(builder.Build());
            main.BackupTo(second);

            long count = second.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H20AsyncSelfCopy\"");
            Assert.Equal(values.Count, count);
            long sum = second.ExecuteScalar<long>("SELECT SUM(\"Value\") FROM \"H20AsyncSelfCopy\"");
            Assert.Equal(values.Sum(), sum);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

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
