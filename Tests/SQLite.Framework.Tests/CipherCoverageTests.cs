#if SQLITECIPHER
using System.Text.Json.Serialization;
using SQLite.Framework.Attributes;
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
