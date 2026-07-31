using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26sBlobRows")]
public class H26sBlobRow
{
    [Key]
    public int Id { get; set; }

    public byte[]? Payload { get; set; }
}

public class H26sBlobOpenCountingDatabase : TestDatabase
{
    private int openCalls;

    public H26sBlobOpenCountingDatabase()
        : base("H26sBlobOpenCounting")
    {
    }

    public int OpenCalls => openCalls;

    public override SQLiteBlobStream OpenBlobStream(string tableName, string columnName, long rowid, bool writable = false, string schema = "main")
    {
        openCalls++;
        return base.OpenBlobStream(tableName, columnName, rowid, writable, schema);
    }
}

public class AsyncBlobStreamOverrideDispatchTests
{
    [Fact]
    public void OpenBlobStreamByTableAndColumnUsesTheOverride()
    {
        using H26sBlobOpenCountingDatabase db = Seeded();

        using SQLiteBlobStream stream = db.OpenBlobStream("H26sBlobRows", "Payload", 1);

        Assert.Equal(4, stream.Length);
        Assert.Equal(1, db.OpenCalls);
    }

    [Fact]
    public async Task OpenBlobStreamAsyncByTableAndColumnUsesTheOverride()
    {
        using H26sBlobOpenCountingDatabase db = Seeded();

        SQLiteBlobStream stream = await db.OpenBlobStreamAsync("H26sBlobRows", "Payload", 1);
        try
        {
            Assert.Equal(4, stream.Length);
        }
        finally
        {
            stream.Dispose();
        }

        Assert.Equal(1, db.OpenCalls);
    }

    [Fact]
    public void OpenBlobStreamByColumnSelectorUsesTheOverride()
    {
        using H26sBlobOpenCountingDatabase db = Seeded();

        using SQLiteBlobStream stream = db.OpenBlobStream<H26sBlobRow>(1, r => r.Payload);

        Assert.Equal(4, stream.Length);
        Assert.Equal(1, db.OpenCalls);
    }

    [Fact]
    public async Task OpenBlobStreamAsyncByColumnSelectorUsesTheOverride()
    {
        using H26sBlobOpenCountingDatabase db = Seeded();

        SQLiteBlobStream stream = await db.OpenBlobStreamAsync<H26sBlobRow>(1, r => r.Payload);
        try
        {
            Assert.Equal(4, stream.Length);
        }
        finally
        {
            stream.Dispose();
        }

        Assert.Equal(1, db.OpenCalls);
    }

    private static H26sBlobOpenCountingDatabase Seeded()
    {
        H26sBlobOpenCountingDatabase db = new();
        db.Execute("CREATE TABLE \"H26sBlobRows\" (\"Id\" INTEGER PRIMARY KEY, \"Payload\" BLOB)");
        db.Execute("INSERT INTO \"H26sBlobRows\" (\"Id\", \"Payload\") VALUES (1, zeroblob(4))");
        return db;
    }
}
