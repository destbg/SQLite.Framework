using System;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TransactionCommitFailureSavepointParityTests
{
    [Fact]
    public void CommitFailure_DoesNotPersistTransactionChanges()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE Parent (Id INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE Child (Id INTEGER PRIMARY KEY, ParentId INTEGER REFERENCES Parent(Id) DEFERRABLE INITIALLY DEFERRED)");
        db.Execute("PRAGMA foreign_keys = ON");

        using (SQLiteTransaction tx = db.BeginTransaction())
        {
            db.Execute("INSERT INTO Child (Id, ParentId) VALUES (1, 999)");
            Assert.ThrowsAny<Exception>(() => tx.Commit());
        }

        long count = db.ExecuteScalar<long>("SELECT COUNT(*) FROM Child");

        Assert.Equal(0, count);
    }
}
