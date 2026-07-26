using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22dBackupOverrideRows")]
public class H22dBackupOverrideRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H22dPathOnlyBackupDatabase : TestDatabase
{
    public H22dPathOnlyBackupDatabase([CallerMemberName] string? methodName = null)
        : base(null, methodName)
    {
    }

    public int PathCalls { get; private set; }

    public override void BackupTo(string destinationPath)
    {
        PathCalls++;
        base.BackupTo(destinationPath);
    }
}

public class H22dBothOverloadsBackupDatabase : TestDatabase
{
    public H22dBothOverloadsBackupDatabase([CallerMemberName] string? methodName = null)
        : base(null, methodName)
    {
    }

    public int PathCalls { get; private set; }

    public int DatabaseCalls { get; private set; }

    public override void BackupTo(string destinationPath)
    {
        PathCalls++;
        base.BackupTo(destinationPath);
    }

    public override void BackupTo(SQLiteDatabase destination, string sourceName = "main", string destName = "main")
    {
        DatabaseCalls++;
        base.BackupTo(destination, sourceName, destName);
    }
}

public class BackupOverrideDispatchTests
{
    [Fact]
    public async Task BackupToAsyncWithAPathRunsTheSameOverrideTheSyncCallRuns()
    {
        using H22dPathOnlyBackupDatabase syncSource = new();
        using H22dPathOnlyBackupDatabase asyncSource = new();
        Seed(syncSource);
        Seed(asyncSource);

        string syncPath = TempPath();
        string asyncPath = TempPath();

        try
        {
            syncSource.BackupTo(syncPath);
            await asyncSource.BackupToAsync(asyncPath, TestContext.Current.CancellationToken);

            Assert.Equal(1, syncSource.PathCalls);
            Assert.Equal(1, asyncSource.PathCalls);
        }
        finally
        {
            Delete(syncPath);
            Delete(asyncPath);
        }
    }

    [Fact]
    public async Task BackupToAsyncWithAPathRunsThePathOverrideWhenBothOverloadsAreOverridden()
    {
        using H22dBothOverloadsBackupDatabase source = new();
        Seed(source);

        string path = TempPath();

        try
        {
            await source.BackupToAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(1, source.PathCalls);
            Assert.Equal(1, source.DatabaseCalls);
        }
        finally
        {
            Delete(path);
        }
    }

    private static void Seed(SQLiteDatabase database)
    {
        database.Table<H22dBackupOverrideRow>().Schema.CreateTable();
        database.Table<H22dBackupOverrideRow>().Add(new H22dBackupOverrideRow { Id = 1, Name = "a" });
    }

    private static string TempPath()
    {
        return Path.Combine(Path.GetTempPath(), $"h22d_backup_{Guid.NewGuid():N}.db3");
    }

    private static void Delete(string path)
    {
        foreach (string candidate in new[] { path, path + "-wal", path + "-shm" })
        {
            try
            {
                if (File.Exists(candidate))
                {
                    File.Delete(candidate);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
