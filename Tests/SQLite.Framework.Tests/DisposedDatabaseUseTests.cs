using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DisposedDatabaseUseTests
{
    [Fact]
    public void CommandsAfterDisposeThrow()
    {
        TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"DisposedRows\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Dispose();

        Assert.Throws<ObjectDisposedException>(() => db.Execute("SELECT 1"));
    }
}
