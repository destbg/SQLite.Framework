using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MultiStatementScalarTests
{
    [Fact]
    public void ExecuteScalarRejectsAMultiStatementBatch()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MsRows\" (\"A\" INTEGER)");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.ExecuteScalar<long>("INSERT INTO \"MsRows\" (\"A\") VALUES (7); SELECT \"A\" FROM \"MsRows\""));

        Assert.Equal("The SQL contains more than one statement, which a query can only run partially. Use Execute for multi-statement batches.", ex.Message);
        Assert.Equal(0, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"MsRows\""));
    }
}
