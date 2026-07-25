using SQLite.Framework.Exceptions;

namespace SQLite.Framework.Tests;

public class OpenConnectionFailureStateTests
{
    [Fact]
    public void FailedOpen_ResetsIsConnecting()
    {
        string missingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "sub", "db.sqlite3");
        SQLiteOptions options = new SQLiteOptionsBuilder(missingDir).Build();
        using SQLiteDatabase db = new(options);

        Assert.Throws<SQLiteException>(() => db.ExecuteScalar<long>("SELECT 1"));

        Assert.False(db.IsConnecting);
        Assert.False(db.IsConnected);
    }
}
