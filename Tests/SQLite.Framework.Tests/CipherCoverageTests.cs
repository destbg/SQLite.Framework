#if SQLITECIPHER
using System.Text.Json.Serialization;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.FTS5;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(CipherJsonPayload))]
internal partial class CipherJsonContext : JsonSerializerContext;

public class CipherCoverageTests
{
    [Fact]
    public void TestDatabase_OpenConnection_AppliesEncryptionKey()
    {
        using TestDatabase db = new();
        long version = db.ExecuteScalar<long>("PRAGMA user_version");
        Assert.Equal(0, version);
    }

    [Fact]
    public void AttachDatabase_WithEncryptionKey_RunsAttachStatement()
    {
        using TestDatabase main = new();
        main.Execute("CREATE TABLE T (Id INTEGER)");

        string auxPath = Path.Combine(Path.GetTempPath(), $"cipher_aux_{Guid.NewGuid():N}.db3");
        try
        {
            SQLiteOptionsBuilder auxBuilder = new(auxPath);
            auxBuilder.UseEncryptionKey("aux-key");
            using (SQLiteDatabase aux = new(auxBuilder.Build()))
            {
                aux.Execute("CREATE TABLE A (Id INTEGER)");
            }

            main.AttachDatabase(auxPath, "aux", "aux-key");
            main.DetachDatabase("aux");
        }
        finally
        {
            if (File.Exists(auxPath))
            {
                File.Delete(auxPath);
            }
        }
    }

    [Fact]
    public void BeginTransaction_SeparateConnection_AppliesEncryptionKeyOnNewConnection()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE T (Id INTEGER)");

        using (SQLiteTransaction tx = db.BeginTransaction(separateConnection: true))
        {
            db.Execute("INSERT INTO T (Id) VALUES (1)");
            tx.Commit();
        }

        long count = db.ExecuteScalar<long>("SELECT COUNT(*) FROM T");
        Assert.Equal(1, count);
    }

    [Fact]
    public void HasJsonConverter_WithSQLiteJsonConverter_ReturnsTrueOnCipherBuild()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:")
            .AddTypeConverter(typeof(CipherJsonPayload), new SQLiteJsonConverter<CipherJsonPayload>(CipherJsonContext.Default.CipherJsonPayload))
            .Build();

        Assert.True(options.HasJsonConverter(typeof(CipherJsonPayload)));
    }

    [Fact]
    public void FtsMappingReader_BuildsTokenizerClause_WithoutTrigramOnCipherBuild()
    {
        FtsTableInfo? info = FtsMappingReader.TryRead(typeof(CipherFtsEntity));
        Assert.NotNull(info);
    }

    [Fact]
    public void CipherVersion_IsReadable()
    {
        using TestDatabase db = new();
        Assert.False(string.IsNullOrEmpty(db.Pragmas.CipherVersion));
    }

    [Fact]
    public void CipherProvider_IsReadable()
    {
        using TestDatabase db = new();
        Assert.False(string.IsNullOrEmpty(db.Pragmas.CipherProvider));
    }

    [Fact]
    public void CipherProviderVersion_IsReadable()
    {
        using TestDatabase db = new();
        Assert.False(string.IsNullOrEmpty(db.Pragmas.CipherProviderVersion));
    }

    [Fact]
    public void CipherCompatibility_SetterRuns()
    {
        using TestDatabase db = new();
        db.Pragmas.CipherCompatibility = 4;
        int _ = db.Pragmas.CipherCompatibility;
    }

    [Fact]
    public void CipherPageSize_SetterRuns()
    {
        using TestDatabase db = new();
        db.Pragmas.CipherPageSize = 4096;
        int _ = db.Pragmas.CipherPageSize;
    }

    [Fact]
    public void CipherUseHmac_SetterRuns()
    {
        using TestDatabase db = new();
        db.Pragmas.CipherUseHmac = true;
        bool _ = db.Pragmas.CipherUseHmac;
        db.Pragmas.CipherUseHmac = false;
    }

    [Fact]
    public void CipherKdfIter_SetterRuns()
    {
        using TestDatabase db = new();
        db.Pragmas.CipherKdfIter = 256000;
        int _ = db.Pragmas.CipherKdfIter;
    }

    [Fact]
    public void CipherMemorySecurity_SetterRuns()
    {
        using TestDatabase db = new();
        db.Pragmas.CipherMemorySecurity = true;
        bool _ = db.Pragmas.CipherMemorySecurity;
        db.Pragmas.CipherMemorySecurity = false;
    }

    [Fact]
    public void Rekey_ChangesEncryptionKey()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cipher_rekey_{Guid.NewGuid():N}.db3");
        try
        {
            SQLiteOptionsBuilder builder = new(path);
            builder.UseEncryptionKey("old-key");
            using (SQLiteDatabase db = new(builder.Build()))
            {
                db.Execute("CREATE TABLE T (Id INTEGER)");
                db.Pragmas.Rekey("new-key");
            }

            SQLiteOptionsBuilder readerBuilder = new(path);
            readerBuilder.UseEncryptionKey("new-key");
            using SQLiteDatabase reader = new(readerBuilder.Build());
            Assert.NotNull(reader.QueryFirst<string>(
                "SELECT name FROM sqlite_master WHERE name = 'T'"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Rekey_NullKey_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentNullException>(() => db.Pragmas.Rekey(null!));
    }

    [Fact]
    public async Task CipherVersionAsync_IsReadable()
    {
        using TestDatabase db = new();
        Assert.False(string.IsNullOrEmpty(await db.Pragmas.GetCipherVersionAsync()));
    }

    [Fact]
    public async Task CipherProviderAsync_IsReadable()
    {
        using TestDatabase db = new();
        Assert.False(string.IsNullOrEmpty(await db.Pragmas.GetCipherProviderAsync()));
    }

    [Fact]
    public async Task CipherProviderVersionAsync_IsReadable()
    {
        using TestDatabase db = new();
        Assert.False(string.IsNullOrEmpty(await db.Pragmas.GetCipherProviderVersionAsync()));
    }

    [Fact]
    public async Task CipherCompatibilityAsync_SetterRuns()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetCipherCompatibilityAsync(4);
        int _ = await db.Pragmas.GetCipherCompatibilityAsync();
    }

    [Fact]
    public async Task CipherPageSizeAsync_SetterRuns()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetCipherPageSizeAsync(4096);
        int _ = await db.Pragmas.GetCipherPageSizeAsync();
    }

    [Fact]
    public async Task CipherUseHmacAsync_SetterRuns()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetCipherUseHmacAsync(true);
        bool _ = await db.Pragmas.GetCipherUseHmacAsync();
    }

    [Fact]
    public async Task CipherKdfIterAsync_SetterRuns()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetCipherKdfIterAsync(256000);
        int _ = await db.Pragmas.GetCipherKdfIterAsync();
    }

    [Fact]
    public async Task CipherMemorySecurityAsync_SetterRuns()
    {
        using TestDatabase db = new();
        await db.Pragmas.SetCipherMemorySecurityAsync(true);
        bool _ = await db.Pragmas.GetCipherMemorySecurityAsync();
        await db.Pragmas.SetCipherMemorySecurityAsync(false);
    }

    [Fact]
    public async Task RekeyAsync_ChangesEncryptionKey()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cipher_rekey_async_{Guid.NewGuid():N}.db3");
        try
        {
            SQLiteOptionsBuilder builder = new(path);
            builder.UseEncryptionKey("old-key");
            using (SQLiteDatabase db = new(builder.Build()))
            {
                db.Execute("CREATE TABLE T (Id INTEGER)");
                await db.Pragmas.RekeyAsync("new-key");
            }

            SQLiteOptionsBuilder readerBuilder = new(path);
            readerBuilder.UseEncryptionKey("new-key");
            using SQLiteDatabase reader = new(readerBuilder.Build());
            Assert.NotNull(reader.QueryFirst<string>(
                "SELECT name FROM sqlite_master WHERE name = 'T'"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}

public class CipherJsonPayload
{
    public string? Name { get; set; }
}

[FullTextSearch]
public class CipherFtsEntity
{
    [FullTextRowId]
    public long RowId { get; set; }

    [FullTextIndexed]
    public string? Body { get; set; }
}
#endif
