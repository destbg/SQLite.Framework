using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H21iBackupCountingDatabase : TestDatabase
{
    private int backupCalls;

    public H21iBackupCountingDatabase()
        : base("H21iBackupCounting")
    {
    }

    public int BackupCalls => backupCalls;

    public override void BackupTo(SQLiteDatabase destination, string sourceName = "main", string destName = "main")
    {
        backupCalls++;
        base.BackupTo(destination, sourceName, destName);
    }
}

public class BackupAsyncOverrideDispatchTests
{
    [Fact]
    public void BackupToAnOpenDestinationUsesTheOverride()
    {
        using H21iBackupCountingDatabase source = Seeded();
        using TestDatabase destination = new();

        source.BackupTo(destination);

        Assert.Equal(1, source.BackupCalls);
        Assert.Equal(1L, destination.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H21iBackupRows\""));
    }

    [Fact]
    public void BackupToAPathUsesTheOverride()
    {
        using H21iBackupCountingDatabase source = Seeded();
        string path = FreshPath();

        try
        {
            source.BackupTo(path);

            Assert.Equal(1, source.BackupCalls);
        }
        finally
        {
            Remove(path);
        }
    }

    [Fact]
    public async Task BackupToAsyncWithAnOpenDestinationUsesTheOverride()
    {
        using H21iBackupCountingDatabase source = Seeded();
        using TestDatabase destination = new();

        await source.BackupToAsync(destination);

        Assert.Equal(1L, destination.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H21iBackupRows\""));
        Assert.Equal(1, source.BackupCalls);
    }

    [Fact]
    public async Task BackupToAsyncWithAPathUsesTheOverride()
    {
        using H21iBackupCountingDatabase source = Seeded();
        string path = FreshPath();

        try
        {
            await source.BackupToAsync(path);

            Assert.Equal(1, source.BackupCalls);
        }
        finally
        {
            Remove(path);
        }
    }

    private static H21iBackupCountingDatabase Seeded()
    {
        H21iBackupCountingDatabase db = new();
        db.Execute("CREATE TABLE \"H21iBackupRows\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER)");
        db.Execute("INSERT INTO \"H21iBackupRows\" (\"Id\", \"Value\") VALUES (1, 10)");
        return db;
    }

    private static string FreshPath()
    {
        return Path.Combine(Path.GetTempPath(), $"h21i_backup_{Guid.NewGuid():N}.db3");
    }

    private static void Remove(string path)
    {
        foreach (string candidate in new[] { path, path + "-wal", path + "-shm" })
        {
            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }
    }
}
