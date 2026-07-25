using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DisposedDatabaseCommandTests
{
    [Fact]
    public void ExecuteReaderAfterTheDatabaseIsDisposedReportsTheDisposedObject()
    {
        SQLiteCommand command = CommandOnDisposedDatabase();

        Assert.Throws<ObjectDisposedException>(() => command.ExecuteReader());
    }

    [Fact]
    public void ExecuteNonQueryAfterTheDatabaseIsDisposedReportsTheDisposedObject()
    {
        SQLiteCommand command = CommandOnDisposedDatabase();

        Assert.Throws<ObjectDisposedException>(() => command.ExecuteNonQuery());
    }

    [Fact]
    public void ExecuteWithLastRowIdAfterTheDatabaseIsDisposedReportsTheDisposedObject()
    {
        SQLiteCommand command = CommandOnDisposedDatabase();

        Assert.Throws<ObjectDisposedException>(() => command.ExecuteWithLastRowId());
    }

    [Fact]
    public void ExecuteQueryAfterTheDatabaseIsDisposedReportsTheDisposedObject()
    {
        SQLiteCommand command = CommandOnDisposedDatabase();

        Assert.Throws<ObjectDisposedException>(() => command.ExecuteQuery<long>().ToList());
    }

    private static SQLiteCommand CommandOnDisposedDatabase()
    {
        TestDatabase db = new();
        db.Execute("CREATE TABLE \"H21iDisposedRows\" (\"Id\" INTEGER PRIMARY KEY)");
        SQLiteCommand command = db.CreateCommand("SELECT COUNT(*) FROM \"H21iDisposedRows\"", []);
        db.Dispose();
        return command;
    }
}
