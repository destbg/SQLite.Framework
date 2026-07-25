using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using PragmaValueParser = SQLite.Framework.Internals.Helpers.PragmaValueParser;

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
        db.Pragmas.JournalMode = SQLiteJournalMode.Delete;
        Assert.Equal(SQLiteJournalMode.Delete, db.Pragmas.JournalMode);
        db.Pragmas.JournalMode = SQLiteJournalMode.Truncate;
        Assert.Equal(SQLiteJournalMode.Truncate, db.Pragmas.JournalMode);
        db.Pragmas.JournalMode = SQLiteJournalMode.Persist;
        Assert.Equal(SQLiteJournalMode.Persist, db.Pragmas.JournalMode);
        db.Pragmas.JournalMode = SQLiteJournalMode.Memory;
        Assert.Equal(SQLiteJournalMode.Memory, db.Pragmas.JournalMode);
        db.Pragmas.JournalMode = SQLiteJournalMode.Off;
        Assert.Equal(SQLiteJournalMode.Off, db.Pragmas.JournalMode);
        db.Pragmas.JournalMode = SQLiteJournalMode.Wal;
        Assert.Equal(SQLiteJournalMode.Wal, db.Pragmas.JournalMode);
    }

    [Fact]
    public void JournalMode_InvalidEnum_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentOutOfRangeException>(() => db.Pragmas.JournalMode = (SQLiteJournalMode)999);
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
        await db.Pragmas.SetJournalModeAsync(SQLiteJournalMode.Wal, TestContext.Current.CancellationToken);
        SQLiteJournalMode mode = await db.Pragmas.GetJournalModeAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SQLiteJournalMode.Wal, mode);
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

    [Fact]
    public void BusyTimeout_Roundtrip()
    {
        using TestDatabase db = new();
        db.Pragmas.BusyTimeout = 2500;
        Assert.Equal(2500, db.Pragmas.BusyTimeout);
        db.Pragmas.BusyTimeout = 0;
        Assert.Equal(0, db.Pragmas.BusyTimeout);
    }

    [Fact]
    public void MmapSize_IsReadable()
    {
        using TestDatabase db = new();
        long original = db.Pragmas.MmapSize;
        Assert.True(original >= 0);
        db.Pragmas.MmapSize = 0;
        Assert.Equal(0, db.Pragmas.MmapSize);
    }

    [Fact]
    public void AutoVacuum_Roundtrip_OnFreshFile()
    {
        using TestDatabase db = new();
        db.Pragmas.AutoVacuum = SQLiteAutoVacuumMode.Incremental;
        Assert.Equal(SQLiteAutoVacuumMode.Incremental, db.Pragmas.AutoVacuum);
    }

    [Fact]
    public void AutoVacuum_DefaultReadsNone()
    {
        using TestDatabase db = new();
        Assert.Equal(SQLiteAutoVacuumMode.None, db.Pragmas.AutoVacuum);
    }

    [Fact]
    public void AutoVacuum_FullMode_Roundtrip()
    {
        using TestDatabase db = new();
        db.Pragmas.AutoVacuum = SQLiteAutoVacuumMode.Full;
        Assert.Equal(SQLiteAutoVacuumMode.Full, db.Pragmas.AutoVacuum);
    }

    [Fact]
    public void IncrementalVacuum_Runs()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.AutoVacuum = SQLiteAutoVacuumMode.Incremental;
        db.Pragmas.IncrementalVacuum();
        db.Pragmas.IncrementalVacuum(10);
    }

    [Fact]
    public void WalAutoCheckpoint_Roundtrip()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.JournalMode = SQLiteJournalMode.Wal;
        db.Pragmas.WalAutoCheckpoint = 500;
        Assert.Equal(500, db.Pragmas.WalAutoCheckpoint);
    }

    [Fact]
    public void WalCheckpoint_ReturnsTrueOnEmptyWal()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.JournalMode = SQLiteJournalMode.Wal;
        Assert.True(db.Pragmas.WalCheckpoint());
        Assert.True(db.Pragmas.WalCheckpoint(SQLiteWalCheckpointMode.Full));
        Assert.True(db.Pragmas.WalCheckpoint(SQLiteWalCheckpointMode.Restart));
        Assert.True(db.Pragmas.WalCheckpoint(SQLiteWalCheckpointMode.Truncate));
    }

    [Fact]
    public void WalCheckpoint_InvalidEnum_Throws()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.JournalMode = SQLiteJournalMode.Wal;
        Assert.Throws<ArgumentOutOfRangeException>(() => db.Pragmas.WalCheckpoint((SQLiteWalCheckpointMode)999));
    }

    [Fact]
    public void IntegrityCheck_OnHealthyDb_ReturnsOk()
    {
        using TestDatabase db = new();
        IReadOnlyList<string> result = db.Pragmas.IntegrityCheck();
        Assert.Single(result);
        Assert.Equal("ok", result[0]);
    }

    [Fact]
    public void QuickCheck_OnHealthyDb_ReturnsOk()
    {
        using TestDatabase db = new();
        IReadOnlyList<string> result = db.Pragmas.QuickCheck();
        Assert.Single(result);
        Assert.Equal("ok", result[0]);
    }

    [Fact]
    public void Optimize_Runs()
    {
        using TestDatabase db = new();
        db.Pragmas.Optimize();
    }

    [Fact]
    public void DeferForeignKeys_Roundtrip()
    {
        using TestDatabase db = new();
        db.Pragmas.DeferForeignKeys = true;
        Assert.True(db.Pragmas.DeferForeignKeys);
        db.Pragmas.DeferForeignKeys = false;
        Assert.False(db.Pragmas.DeferForeignKeys);
    }

    [Fact]
    public void Encoding_IsReadable()
    {
        using TestDatabase db = new();
        SQLiteEncoding encoding = db.Pragmas.Encoding;
        Assert.True(encoding == SQLiteEncoding.Utf8 || encoding == SQLiteEncoding.Utf16
            || encoding == SQLiteEncoding.Utf16le || encoding == SQLiteEncoding.Utf16be);
    }

    [Fact]
    public void Encoding_OnFreshFile_AcceptsExplicitWrite()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.Encoding = SQLiteEncoding.Utf8;
        Assert.Equal(SQLiteEncoding.Utf8, db.Pragmas.Encoding);
    }

    [Fact]
    public void Encoding_Utf16le_OnFreshFile_RoundTrips()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.Encoding = SQLiteEncoding.Utf16le;
        Assert.Equal(SQLiteEncoding.Utf16le, db.Pragmas.Encoding);
    }

    [Fact]
    public void Encoding_Utf16be_OnFreshFile_RoundTrips()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.Encoding = SQLiteEncoding.Utf16be;
        Assert.Equal(SQLiteEncoding.Utf16be, db.Pragmas.Encoding);
    }

    [Fact]
    public void Encoding_Utf16_OnFreshFile_BecomesPlatformVariant()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.Encoding = SQLiteEncoding.Utf16;
        SQLiteEncoding result = db.Pragmas.Encoding;
        Assert.True(result == SQLiteEncoding.Utf16le || result == SQLiteEncoding.Utf16be);
    }

    [Fact]
    public void Encoding_InvalidEnum_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentOutOfRangeException>(() => db.Pragmas.Encoding = (SQLiteEncoding)999);
    }

    [Fact]
    public void LockingMode_Roundtrip()
    {
        using TestDatabase db = new();
        db.Pragmas.LockingMode = SQLiteLockingMode.Exclusive;
        Assert.Equal(SQLiteLockingMode.Exclusive, db.Pragmas.LockingMode);
        db.Pragmas.LockingMode = SQLiteLockingMode.Normal;
        Assert.Equal(SQLiteLockingMode.Normal, db.Pragmas.LockingMode);
    }

    [Fact]
    public void LockingMode_InvalidEnum_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentOutOfRangeException>(() => db.Pragmas.LockingMode = (SQLiteLockingMode)999);
    }

    [Fact]
    public void ApplicationId_Roundtrip()
    {
        using TestDatabase db = new();
        db.Pragmas.ApplicationId = 0x42424242;
        Assert.Equal(0x42424242, db.Pragmas.ApplicationId);
    }

    [Fact]
    public void DataVersion_IsReadable()
    {
        using TestDatabase db = new();
        int v = db.Pragmas.DataVersion;
        Assert.True(v >= 0);
    }

    [Fact]
    public void SchemaVersion_IsReadable()
    {
        using TestDatabase db = new();
        int v = db.Pragmas.SchemaVersion;
        Assert.True(v >= 0);
    }

    [Fact]
    public async Task BusyTimeoutAsync_Roundtrip()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetBusyTimeoutAsync(1500);
        Assert.Equal(1500, await db.Pragmas.GetBusyTimeoutAsync());
    }

    [Fact]
    public async Task MmapSizeAsync_IsReadable()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetMmapSizeAsync(0);
        Assert.Equal(0, await db.Pragmas.GetMmapSizeAsync());
    }

    [Fact]
    public async Task AutoVacuumAsync_Roundtrip()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetAutoVacuumAsync(SQLiteAutoVacuumMode.Incremental);
        Assert.Equal(SQLiteAutoVacuumMode.Incremental, await db.Pragmas.GetAutoVacuumAsync());
    }

    [Fact]
    public async Task IncrementalVacuumAsync_Runs()
    {
        using TestDatabase db = new(useFile: true);
        await db.Pragmas.SetAutoVacuumAsync(SQLiteAutoVacuumMode.Incremental);
        await db.Pragmas.IncrementalVacuumAsync();
        await db.Pragmas.IncrementalVacuumAsync(5);
    }

    [Fact]
    public async Task WalAutoCheckpointAsync_Roundtrip()
    {
        using TestDatabase db = new(useFile: true);
        await db.Pragmas.SetJournalModeAsync(SQLiteJournalMode.Wal);
        await db.Pragmas.SetWalAutoCheckpointAsync(800);
        Assert.Equal(800, await db.Pragmas.GetWalAutoCheckpointAsync());
    }

    [Fact]
    public async Task WalCheckpointAsync_ReturnsTrueOnEmptyWal()
    {
        using TestDatabase db = new(useFile: true);
        await db.Pragmas.SetJournalModeAsync(SQLiteJournalMode.Wal);
        Assert.True(await db.Pragmas.WalCheckpointAsync());
        Assert.True(await db.Pragmas.WalCheckpointAsync(SQLiteWalCheckpointMode.Truncate));
    }

    [Fact]
    public async Task IntegrityCheckAsync_OnHealthyDb_ReturnsOk()
    {
        using TestDatabase db = new();
        IReadOnlyList<string> result = await db.Pragmas.IntegrityCheckAsync();
        Assert.Single(result);
        Assert.Equal("ok", result[0]);
    }

    [Fact]
    public async Task QuickCheckAsync_OnHealthyDb_ReturnsOk()
    {
        using TestDatabase db = new();
        IReadOnlyList<string> result = await db.Pragmas.QuickCheckAsync();
        Assert.Single(result);
        Assert.Equal("ok", result[0]);
    }

    [Fact]
    public async Task OptimizeAsync_Runs()
    {
        using TestDatabase db = new();
        await db.Pragmas.OptimizeAsync();
    }

    [Fact]
    public async Task DeferForeignKeysAsync_Roundtrip()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetDeferForeignKeysAsync(true);
        Assert.True(await db.Pragmas.GetDeferForeignKeysAsync());
    }

    [Fact]
    public async Task EncodingAsync_IsReadable()
    {
        using TestDatabase db = new();
        SQLiteEncoding encoding = await db.Pragmas.GetEncodingAsync();
        Assert.True(encoding == SQLiteEncoding.Utf8 || encoding == SQLiteEncoding.Utf16
            || encoding == SQLiteEncoding.Utf16le || encoding == SQLiteEncoding.Utf16be);
    }

    [Fact]
    public async Task EncodingAsync_OnFreshFile_AcceptsExplicitWrite()
    {
        using TestDatabase db = new(useFile: true);
        await db.Pragmas.SetEncodingAsync(SQLiteEncoding.Utf8);
        Assert.Equal(SQLiteEncoding.Utf8, await db.Pragmas.GetEncodingAsync());
    }

    [Fact]
    public async Task LockingModeAsync_Roundtrip()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetLockingModeAsync(SQLiteLockingMode.Exclusive);
        Assert.Equal(SQLiteLockingMode.Exclusive, await db.Pragmas.GetLockingModeAsync());
        await db.Pragmas.SetLockingModeAsync(SQLiteLockingMode.Normal);
        Assert.Equal(SQLiteLockingMode.Normal, await db.Pragmas.GetLockingModeAsync());
    }

    [Fact]
    public async Task ApplicationIdAsync_Roundtrip()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetApplicationIdAsync(0x12345678);
        Assert.Equal(0x12345678, await db.Pragmas.GetApplicationIdAsync());
    }

    [Fact]
    public async Task DataVersionAsync_IsReadable()
    {
        using TestDatabase db = new();
        int v = await db.Pragmas.GetDataVersionAsync();
        Assert.True(v >= 0);
    }

    [Fact]
    public async Task SchemaVersionAsync_IsReadable()
    {
        using TestDatabase db = new();
        int v = await db.Pragmas.GetSchemaVersionAsync();
        Assert.True(v >= 0);
    }

    [Fact]
    public void ParseEncoding_UnknownString_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => PragmaValueParser.ParseEncoding("garbage"));
        Assert.Contains("garbage", ex.Message);
    }

    [Fact]
    public void ParseEncoding_Null_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => PragmaValueParser.ParseEncoding(null));
        Assert.Contains("<null>", ex.Message);
    }

    [Fact]
    public void ParseLockingMode_UnknownString_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => PragmaValueParser.ParseLockingMode("weird"));
        Assert.Contains("weird", ex.Message);
    }

    [Fact]
    public void ParseLockingMode_Null_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => PragmaValueParser.ParseLockingMode(null));
        Assert.Contains("<null>", ex.Message);
    }

    [Fact]
    public void ParseJournalMode_UnknownString_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => PragmaValueParser.ParseJournalMode("garbage"));
        Assert.Contains("garbage", ex.Message);
    }

    [Fact]
    public void ParseJournalMode_Null_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => PragmaValueParser.ParseJournalMode(null));
        Assert.Contains("<null>", ex.Message);
    }

    private sealed class CustomPragmas : SQLitePragmas
    {
        public CustomPragmas(SQLiteDatabase database) : base(database) { }
        public string CustomLabel => "custom";
    }
}
