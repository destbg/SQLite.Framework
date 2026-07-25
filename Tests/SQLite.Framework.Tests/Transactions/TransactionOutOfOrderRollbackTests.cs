using System;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TransactionOutOfOrderRollbackTests
{
    [Fact]
    public void InnerRollbackAfterOuterRollbackDoesNotThrow()
    {
        using TestDatabase db = new();
        SQLiteTransaction outer = db.BeginTransaction();
        SQLiteTransaction inner = db.BeginTransaction();
        outer.Rollback();

        Exception? ex = Record.Exception(() => inner.Rollback());

        Assert.Null(ex);
    }

    [Fact]
    public void InnerCommitAfterOuterRollbackDoesNotThrow()
    {
        using TestDatabase db = new();
        SQLiteTransaction outer = db.BeginTransaction();
        SQLiteTransaction inner = db.BeginTransaction();
        outer.Rollback();

        Exception? ex = Record.Exception(() => inner.Commit());

        Assert.Null(ex);
    }
}
