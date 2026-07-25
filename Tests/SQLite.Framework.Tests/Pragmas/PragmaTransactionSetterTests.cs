using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PragmaTransactionSetterTests
{
    [Fact]
    public void ForeignKeysSetInsideATransactionThrows()
    {
        using TestDatabase db = new(useFile: true);
        db.Pragmas.ForeignKeys = false;

        using (SQLiteTransaction tx = db.BeginTransaction())
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Pragmas.ForeignKeys = true);
            Assert.Equal("PRAGMA foreign_keys cannot change inside a transaction. Set it before the transaction starts.", ex.Message);
            tx.Commit();
        }

        db.Pragmas.ForeignKeys = true;
        Assert.True(db.Pragmas.ForeignKeys);
    }
}
