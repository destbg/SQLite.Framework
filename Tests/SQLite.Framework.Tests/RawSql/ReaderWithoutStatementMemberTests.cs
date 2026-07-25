using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReaderWithoutStatementMemberTests
{
    [Fact]
    public void CommentOnlySqlReaderReportsNoColumns()
    {
        using TestDatabase db = new();
        SQLiteCommand command = db.CreateCommand("-- nothing here", []);
        using SQLiteDataReader reader = command.ExecuteReader();

        Assert.Equal(0, reader.FieldCount);
    }

    [Fact]
    public void EmptySqlReaderReportsNoColumns()
    {
        using TestDatabase db = new();
        SQLiteCommand command = db.CreateCommand("   ", []);
        using SQLiteDataReader reader = command.ExecuteReader();

        Assert.Equal(0, reader.FieldCount);
    }

    [Fact]
    public void SemicolonOnlySqlReaderReportsNoColumns()
    {
        using TestDatabase db = new();
        SQLiteCommand command = db.CreateCommand(";;", []);
        using SQLiteDataReader reader = command.ExecuteReader();

        Assert.Equal(0, reader.FieldCount);
    }

    [Fact]
    public void CommentOnlySqlReaderColumnTypeMatchesOutOfRangeBehavior()
    {
        using TestDatabase db = new();
        SQLiteCommand withColumn = db.CreateCommand("SELECT 1 AS \"A\"", []);
        SQLiteColumnType outOfRange;
        using (SQLiteDataReader columnReader = withColumn.ExecuteReader())
        {
            Assert.True(columnReader.Read());
            outOfRange = columnReader.GetColumnType(5);
        }

        SQLiteCommand command = db.CreateCommand("-- nothing here", []);
        using SQLiteDataReader reader = command.ExecuteReader();

        Assert.Equal(outOfRange, reader.GetColumnType(0));
    }

    [Fact]
    public void RealStatementReaderReportsItsColumns()
    {
        using TestDatabase db = new();
        SQLiteCommand command = db.CreateCommand("SELECT 1 AS \"A\", 2 AS \"B\"", []);
        using SQLiteDataReader reader = command.ExecuteReader();

        Assert.Equal(2, reader.FieldCount);
    }
}
