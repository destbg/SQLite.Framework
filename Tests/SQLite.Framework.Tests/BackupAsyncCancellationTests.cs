using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20AsyncBusyRows")]
public class H20AsyncBusyRow
{
    [Key]
    public int Id { get; set; }
}

public class BackupAsyncCancellationTests
{
    [Fact]
    public async Task BackupToAsyncHonorsCancellationWhileDestinationBusy()
    {
        using TestDatabase source = new(useFile: true);
        source.Execute("CREATE TABLE \"H20AsyncCancelSrc\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");
        source.Execute("INSERT INTO \"H20AsyncCancelSrc\" (\"Id\", \"Value\") VALUES (1, 1)");

        using TestDatabase destination = new(b => b.UseWalMode(), useFile: true);
        destination.Execute("CREATE TABLE \"H20AsyncCancelDest\" (\"Id\" INTEGER PRIMARY KEY)");

        SQLiteOptionsBuilder writerBuilder = new(destination.Options.DatabasePath);
#if SQLITECIPHER
        writerBuilder.UseEncryptionKey("test-key");
#endif
        using SQLiteDatabase writer = new(writerBuilder.Build());
        writer.Execute("PRAGMA busy_timeout = 0");
        SQLiteTransaction held = writer.BeginTransaction();
        writer.Execute("INSERT INTO \"H20AsyncCancelDest\" (\"Id\") VALUES (1)");

        try
        {
            using CancellationTokenSource cts = new();
            Task backup = source.BackupToAsync(destination, ct: cts.Token);
            await Task.Delay(1000);
            Assert.False(backup.IsCompleted);

            cts.Cancel();

            Task winner = await Task.WhenAny(backup, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.True(winner == backup);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => backup);
        }
        finally
        {
            held.Rollback();
        }
    }

    [Fact]
    public async Task BackupToAsyncToPathWaitsWhileDestinationBusy()
    {
        using TestDatabase source = new(useFile: true);
        source.Execute("CREATE TABLE \"H20AsyncBusyRows\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");
        source.Execute("INSERT INTO \"H20AsyncBusyRows\" (\"Id\", \"Value\") VALUES (1, 1)");

        string destinationPath = $"{nameof(BackupToAsyncToPathWaitsWhileDestinationBusy)}_{Guid.NewGuid():N}.db3";
        SQLiteOptionsBuilder destinationBuilder = new(destinationPath);
        destinationBuilder.UseWalMode();
#if SQLITECIPHER
        destinationBuilder.UseEncryptionKey("test-key");
#endif
        using (SQLiteDatabase destination = new(destinationBuilder.Build()))
        {
            destination.Schema.CreateTable<H20AsyncBusyRow>();
        }

        SQLiteOptionsBuilder writerBuilder = new(destinationPath);
#if SQLITECIPHER
        writerBuilder.UseEncryptionKey("test-key");
#endif
        using SQLiteDatabase writer = new(writerBuilder.Build());
        writer.Execute("PRAGMA busy_timeout = 0");
        SQLiteTransaction held = writer.BeginTransaction();
        writer.Execute("INSERT INTO \"H20AsyncBusyRows\" (\"Id\") VALUES (2)");

        bool rolledBack = false;
        try
        {
            Task backup = source.BackupToAsync(destinationPath);
            await Task.Delay(1000);
            Assert.False(backup.IsCompleted);

            held.Rollback();
            rolledBack = true;
            await backup.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            if (!rolledBack)
            {
                held.Rollback();
            }

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
        }

        Assert.Equal(1, source.Table<H20AsyncBusyRow>().Single().Id);
    }
}
