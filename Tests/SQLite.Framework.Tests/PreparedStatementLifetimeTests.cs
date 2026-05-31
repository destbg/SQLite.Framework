using SQLitePCL;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PreparedStatementLifetimeTests
{
    private static int CountOpenStatements(SQLiteDatabase db)
    {
        sqlite3 handle = db.GetActiveHandle();
        int count = 0;
        sqlite3_stmt? stmt = raw.sqlite3_next_stmt(handle, null);
        while (stmt != null)
        {
            count++;
            stmt = raw.sqlite3_next_stmt(handle, stmt);
        }

        return count;
    }

    [Fact]
    public void BindFailureDoesNotLeakPreparedStatement()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE StmtLifetime (Id INTEGER)");
        db.GetActiveHandle().enable_sqlite3_next_stmt(true);

        int before = CountOpenStatements(db);

        Assert.ThrowsAny<Exception>(() =>
            db.Execute("INSERT INTO StmtLifetime (Id) VALUES (1)", new SQLiteParameter { Name = "@missing", Value = 1 }));

        int after = CountOpenStatements(db);

        Assert.Equal(before, after);
    }
}
