using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NoStatementSqlExecutionTests
{
    [Fact]
    public void ExecuteScalarOverEmptySqlReturnsDefault()
    {
        using TestDatabase db = new();

        Assert.Equal(0, db.Execute(""));
        Assert.Equal(0L, db.ExecuteScalar<long>(""));
    }

    [Fact]
    public void ExecuteScalarOverCommentOnlySqlReturnsDefault()
    {
        using TestDatabase db = new();

        Assert.Equal(0, db.Execute("-- nothing here"));
        Assert.Equal(0L, db.ExecuteScalar<long>("-- nothing here"));
    }

    [Fact]
    public void ExecuteScalarOverSeparatorsOnlySqlReturnsDefault()
    {
        using TestDatabase db = new();

        Assert.Equal(0, db.Execute(" ; ; "));
        Assert.Equal(0L, db.ExecuteScalar<long>(" ; ; "));
    }

    [Fact]
    public void ExecuteReaderOverCommentOnlySqlReadsNoRows()
    {
        using TestDatabase db = new();

        SQLiteCommand command = db.CreateCommand("-- nothing here", []);
        using SQLiteDataReader reader = command.ExecuteReader();

        Assert.False(reader.Read());
    }

    [Fact]
    public void QueryOverCommentOnlySqlReturnsNoRows()
    {
        using TestDatabase db = new();

        Assert.Empty(db.Query<long>("-- nothing here"));
    }

    [Fact]
    public void ExecuteWithLastRowIdOverEmptySqlReturnsZeroChanges()
    {
        using TestDatabase db = new();

        (int changes, long rowId) = db.CreateCommand(" ; ", []).ExecuteWithLastRowId();

        Assert.Equal(0, changes);
        Assert.Equal(0, rowId);
    }
}
