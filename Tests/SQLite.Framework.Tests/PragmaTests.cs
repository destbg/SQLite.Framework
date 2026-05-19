using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PragmaTests
{
    [Fact]
    public void ForeignKeys_Roundtrip()
    {
        using TestDatabase db = new();
        db.Pragmas.ForeignKeys = true;
        Assert.True(db.Pragmas.ForeignKeys);
        db.Pragmas.ForeignKeys = false;
        Assert.False(db.Pragmas.ForeignKeys);
    }

    [Fact]
    public void JournalMode_Roundtrip()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.JournalMode = "WAL";
        Assert.Equal("wal", db.Pragmas.JournalMode);
        db.Pragmas.JournalMode = "DELETE";
        Assert.Equal("delete", db.Pragmas.JournalMode);
    }

    [Fact]
    public void CacheSize_Roundtrip()
    {
        using TestDatabase db = new();
        db.Pragmas.CacheSize = -4000;
        Assert.Equal(-4000, db.Pragmas.CacheSize);
    }

    [Fact]
    public void SynchronousMode_Roundtrip()
    {
        using TestDatabase db = new();
        db.Pragmas.SynchronousMode = SQLiteSynchronousMode.Normal;
        Assert.Equal(SQLiteSynchronousMode.Normal, db.Pragmas.SynchronousMode);
        db.Pragmas.SynchronousMode = SQLiteSynchronousMode.Full;
        Assert.Equal(SQLiteSynchronousMode.Full, db.Pragmas.SynchronousMode);
        db.Pragmas.SynchronousMode = SQLiteSynchronousMode.Off;
        Assert.Equal(SQLiteSynchronousMode.Off, db.Pragmas.SynchronousMode);
    }

    [Fact]
    public void UserVersion_Roundtrip()
    {
        using TestDatabase db = new();
        db.Pragmas.UserVersion = 42;
        Assert.Equal(42, db.Pragmas.UserVersion);
    }

    [Fact]
    public void PageSize_IsReadable()
    {
        using TestDatabase db = new();
        Assert.True(db.Pragmas.PageSize > 0);
    }

    [Fact]
    public void FreelistCount_IsReadable()
    {
        using TestDatabase db = new();
        Assert.True(db.Pragmas.FreelistCount >= 0);
    }

    [Fact]
    public void RecursiveTriggers_Roundtrip()
    {
        using TestDatabase db = new();
        db.Pragmas.RecursiveTriggers = true;
        Assert.True(db.Pragmas.RecursiveTriggers);
        db.Pragmas.RecursiveTriggers = false;
        Assert.False(db.Pragmas.RecursiveTriggers);
    }

    [Fact]
    public void TempStore_Roundtrip()
    {
        using TestDatabase db = new();
        db.Pragmas.TempStore = 2;
        Assert.Equal(2, db.Pragmas.TempStore);
    }

    [Fact]
    public void SecureDelete_Roundtrip()
    {
        using TestDatabase db = new();
        db.Pragmas.SecureDelete = true;
        Assert.True(db.Pragmas.SecureDelete);
        db.Pragmas.SecureDelete = false;
        Assert.False(db.Pragmas.SecureDelete);
    }

    [Fact]
    public void UsePragmas_CustomFactory_RegistersSubclass()
    {
        using TestDatabase db = new(b => b.UsePragmas(d => new CustomPragmas(d)));
        Assert.IsType<CustomPragmas>(db.Pragmas);
        Assert.Equal("custom", ((CustomPragmas)db.Pragmas).CustomLabel);
    }

    [Fact]
    public void UsePragmas_NullFactory_Throws()
    {
        SQLiteOptionsBuilder builder = new("test.db");
        Assert.Throws<ArgumentNullException>(() => builder.UsePragmas(null!));
    }

    [Fact]
    public async Task AsyncExtensions_RoundtripUserVersion()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetUserVersionAsync(11, TestContext.Current.CancellationToken);
        int version = await db.Pragmas.GetUserVersionAsync(TestContext.Current.CancellationToken);
        Assert.Equal(11, version);
    }

    [Fact]
    public async Task AsyncExtensions_RoundtripJournalMode()
    {
        using TestDatabase db = new(useFile: true);
        await db.Pragmas.SetJournalModeAsync("WAL", TestContext.Current.CancellationToken);
        string mode = await db.Pragmas.GetJournalModeAsync(TestContext.Current.CancellationToken);
        Assert.Equal("wal", mode);
    }

    [Fact]
    public async Task AsyncExtensions_RoundtripForeignKeys()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetForeignKeysAsync(true, TestContext.Current.CancellationToken);
        bool on = await db.Pragmas.GetForeignKeysAsync(TestContext.Current.CancellationToken);
        Assert.True(on);
    }

    [Fact]
    public async Task AsyncExtensions_RoundtripCacheSize()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetCacheSizeAsync(-2000, TestContext.Current.CancellationToken);
        int size = await db.Pragmas.GetCacheSizeAsync(TestContext.Current.CancellationToken);
        Assert.Equal(-2000, size);
    }

    [Fact]
    public async Task AsyncExtensions_RoundtripSynchronousMode()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetSynchronousModeAsync(SQLiteSynchronousMode.Normal, TestContext.Current.CancellationToken);
        SQLiteSynchronousMode mode = await db.Pragmas.GetSynchronousModeAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SQLiteSynchronousMode.Normal, mode);
    }

    [Fact]
    public async Task AsyncExtensions_PageSize_IsReadable()
    {
        using TestDatabase db = new();
        long size = await db.Pragmas.GetPageSizeAsync(TestContext.Current.CancellationToken);
        Assert.True(size > 0);
    }

    [Fact]
    public async Task AsyncExtensions_FreelistCount_IsReadable()
    {
        using TestDatabase db = new();
        long count = await db.Pragmas.GetFreelistCountAsync(TestContext.Current.CancellationToken);
        Assert.True(count >= 0);
    }

    [Fact]
    public async Task AsyncExtensions_RoundtripRecursiveTriggers()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetRecursiveTriggersAsync(true, TestContext.Current.CancellationToken);
        bool on = await db.Pragmas.GetRecursiveTriggersAsync(TestContext.Current.CancellationToken);
        Assert.True(on);
    }

    [Fact]
    public async Task AsyncExtensions_RoundtripTempStore()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetTempStoreAsync(2, TestContext.Current.CancellationToken);
        int store = await db.Pragmas.GetTempStoreAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, store);
    }

    [Fact]
    public async Task AsyncExtensions_RoundtripSecureDelete()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetSecureDeleteAsync(true, TestContext.Current.CancellationToken);
        bool on = await db.Pragmas.GetSecureDeleteAsync(TestContext.Current.CancellationToken);
        Assert.True(on);
    }

    [Fact]
    public void Sequence_AccessedTwice_ReturnsSameInstance()
    {
        using TestDatabase db = new();
        ReadOnlySQLiteTable<SQLite.Framework.Models.SQLiteSequence> a = db.Pragmas.Sequence;
        ReadOnlySQLiteTable<SQLite.Framework.Models.SQLiteSequence> b = db.Pragmas.Sequence;
        Assert.Same(a, b);
    }

    [Fact]
    public void Master_AccessedTwice_ReturnsSameInstance()
    {
        using TestDatabase db = new();
        ReadOnlySQLiteTable<SQLite.Framework.Models.SQLiteMaster> a = db.Pragmas.Master;
        ReadOnlySQLiteTable<SQLite.Framework.Models.SQLiteMaster> b = db.Pragmas.Master;
        Assert.Same(a, b);
    }

    private sealed class CustomPragmas : SQLitePragmas
    {
        public CustomPragmas(SQLiteDatabase database) : base(database) { }
        public string CustomLabel => "custom";
    }
}
