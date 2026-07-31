using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BlobStreamFlushAfterDisposeTests
{
    [Fact]
    public void FlushAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H26sFlushRows\" (\"Id\" INTEGER PRIMARY KEY, \"Payload\" BLOB)");
        db.Execute("INSERT INTO \"H26sFlushRows\" (\"Id\", \"Payload\") VALUES (1, zeroblob(4))");

        SQLiteBlobStream stream = db.OpenBlobStream("H26sFlushRows", "Payload", 1, writable: true);
        stream.Dispose();

        Assert.Throws<ObjectDisposedException>(() => stream.Flush());
    }
}
