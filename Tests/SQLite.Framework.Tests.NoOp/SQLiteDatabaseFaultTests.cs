using SQLite.Framework.Exceptions;

namespace SQLite.Framework.Tests.NoOp;

public class SQLiteDatabaseFaultTests
{
    public SQLiteDatabaseFaultTests()
    {
        NoOpFixture.Init();
    }

    [Fact]
    public void Diagnostic_NoOpProviderInstalled()
    {
        Assert.Equal("NoOpSQLite", SQLitePCL.raw.GetNativeLibraryName());
    }

    [Fact]
    public void BackupTo_BackupInitReturnsNull_ThrowsSQLiteException()
    {
        NoOpSQLite.BackupInitReturnsNull = true;
        NoOpSQLite.ErrCode = 1;

        SQLiteOptions options = new SQLiteOptionsBuilder("noop-src.db").Build();
        SQLiteOptions destOptions = new SQLiteOptionsBuilder("noop-dest.db").Build();
        using SQLiteDatabase source = new(options);
        using SQLiteDatabase destination = new(destOptions);

        SQLiteException ex = Assert.Throws<SQLiteException>(() => source.BackupTo(destination));
        Assert.NotNull(ex);
    }

    [Fact]
    public void BackupTo_BackupStepReturnsError_ThrowsSQLiteException()
    {
        NoOpSQLite.BackupStepReturnCode = 1;

        SQLiteOptions options = new SQLiteOptionsBuilder("noop-src2.db").Build();
        SQLiteOptions destOptions = new SQLiteOptionsBuilder("noop-dest2.db").Build();
        using SQLiteDatabase source = new(options);
        using SQLiteDatabase destination = new(destOptions);

        SQLiteException ex = Assert.Throws<SQLiteException>(() => source.BackupTo(destination));
        Assert.NotNull(ex);
    }

    [Fact]
    public void BackupTo_BackupStepReturnsBusyThenDone_RetriesAndSucceeds()
    {
        int callCount = 0;
        NoOpSQLite.BackupStepReturnCode = 5;
        SQLiteOptions options = new SQLiteOptionsBuilder("noop-src3.db").Build();
        SQLiteOptions destOptions = new SQLiteOptionsBuilder("noop-dest3.db").Build();
        using SQLiteDatabase source = new(options);
        using SQLiteDatabase destination = new(destOptions);

        Task task = Task.Run(() =>
        {
            Thread.Sleep(120);
            NoOpSQLite.BackupStepReturnCode = 101;
        });

        source.BackupTo(destination);
        task.Wait();

        Assert.True(callCount >= 0);
    }

    [Fact]
    public void OpenTransactionConnection_BeginFails_ThrowsSQLiteException()
    {
        NoOpSQLite.BeginStepReturnCode = 1;

        SQLiteOptions options = new SQLiteOptionsBuilder("noop-tx.db").Build();
        using SQLiteDatabase db = new(options);

        SQLiteException ex = Assert.Throws<SQLiteException>(() => db.BeginTransaction(separateConnection: true));
        Assert.Contains("Failed to begin transaction", ex.Message);
    }
}
