using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21lLedgers")]
public class H21lLedger
{
    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";
}

public class TransactionRollbackStatementFailureTests
{
    [Fact]
    public void DisposeAfterAFailingRollbackStatementLeavesNoRow()
    {
        using TestDatabase db = new(b => b.AddCommandInterceptor(new H21lRollbackBlockingInterceptor()));
        db.Table<H21lLedger>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() =>
        {
            using SQLiteTransaction transaction = db.BeginTransaction();
            db.Table<H21lLedger>().Add(new H21lLedger { Id = 1, Note = "a" });
        });

        Assert.Empty(db.Table<H21lLedger>().ToList());
    }

    [Fact]
    public void ExplicitRollbackThatFailsLeavesNoRow()
    {
        using TestDatabase db = new(b => b.AddCommandInterceptor(new H21lRollbackBlockingInterceptor()));
        db.Table<H21lLedger>().Schema.CreateTable();

        using SQLiteTransaction transaction = db.BeginTransaction();
        db.Table<H21lLedger>().Add(new H21lLedger { Id = 1, Note = "a" });
        Assert.Throws<InvalidOperationException>(transaction.Rollback);

        Assert.Empty(db.Table<H21lLedger>().ToList());
    }

    [Fact]
    public void ConnectionIsOutOfAnyTransactionAfterAFailingRollbackStatement()
    {
        using TestDatabase db = new(b => b.AddCommandInterceptor(new H21lRollbackBlockingInterceptor()));
        db.Table<H21lLedger>().Schema.CreateTable();

        Assert.Throws<InvalidOperationException>(() =>
        {
            using SQLiteTransaction transaction = db.BeginTransaction();
            db.Table<H21lLedger>().Add(new H21lLedger { Id = 1, Note = "a" });
        });

        db.Pragmas.ForeignKeys = false;

        Assert.False(db.Pragmas.ForeignKeys);
    }

    [Fact]
    public void DisposeWithAPassiveInterceptorLeavesNoRow()
    {
        using TestDatabase db = new(b => b.AddCommandInterceptor(new H21lPassiveInterceptor()));
        db.Table<H21lLedger>().Schema.CreateTable();

        using (SQLiteTransaction transaction = db.BeginTransaction())
        {
            db.Table<H21lLedger>().Add(new H21lLedger { Id = 1, Note = "a" });
        }

        db.Pragmas.ForeignKeys = false;

        Assert.Empty(db.Table<H21lLedger>().ToList());
        Assert.False(db.Pragmas.ForeignKeys);
    }

    private sealed class H21lRollbackBlockingInterceptor : ISQLiteCommandInterceptor
    {
        public void OnExecuting(SQLiteCommand command)
        {
            if (command.CommandText.StartsWith("ROLLBACK TO ", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The interceptor rejected the rollback statement.");
            }
        }

        public void OnExecuted(SQLiteCommand command, int? rowsAffected)
        {
        }

        public void OnFailed(SQLiteCommand command, Exception exception)
        {
        }

        public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
        {
        }

        public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount)
        {
        }
    }

    private sealed class H21lPassiveInterceptor : ISQLiteCommandInterceptor
    {
        public void OnExecuting(SQLiteCommand command)
        {
        }

        public void OnExecuted(SQLiteCommand command, int? rowsAffected)
        {
        }

        public void OnFailed(SQLiteCommand command, Exception exception)
        {
        }

        public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
        {
        }

        public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount)
        {
        }
    }
}
