using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SavepointDisposeOrderTests
{
    [Fact]
    public void DisposingTheInnerTransactionAfterTheOuterDoesNotThrow()
    {
        using TestDatabase db = new(useFile: true);
        SQLiteTransaction outer = db.BeginTransaction();
        SQLiteTransaction inner = db.BeginTransaction();

        outer.Dispose();
        Exception? ex = Record.Exception(() => inner.Dispose());

        Assert.Null(ex);
    }
}
