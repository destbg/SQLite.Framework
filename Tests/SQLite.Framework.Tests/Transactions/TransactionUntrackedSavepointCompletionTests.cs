using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TransactionUntrackedSavepointCompletionTests
{
    [Fact]
    public void CommittingAnUntrackedSavepointLeavesOtherTransactionsAlone()
    {
        using TestDatabase db = new(null, nameof(CommittingAnUntrackedSavepointLeavesOtherTransactionsAlone));
        db.Execute("CREATE TABLE \"UntrackedRows\" (\"Id\" INTEGER PRIMARY KEY)");

        SQLiteTransaction outer = db.BeginTransaction();
        db.Execute("SAVEPOINT untracked_sp");
        SQLiteTransaction untracked = new(db, "untracked_sp", ownsLock: false);
        db.RemoveSavepoint(untracked);

        db.Execute("INSERT INTO \"UntrackedRows\" (\"Id\") VALUES (1)");
        untracked.Commit();

        outer.Rollback();

        Assert.Equal(0, db.ExecuteScalar<int>("SELECT COUNT(*) FROM \"UntrackedRows\""));
    }
}
