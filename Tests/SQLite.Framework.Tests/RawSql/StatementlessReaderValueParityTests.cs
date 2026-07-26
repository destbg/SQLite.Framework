using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StatementlessReaderValueParityTests
{
    [Fact]
    public void IntegerValueOnACommentOnlyReaderMatchesOutOfRangeBehavior()
    {
        using TestDatabase db = new();

        object? expected = OutOfRangeValue(db, SQLiteColumnType.Integer, typeof(long));

        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        Assert.Equal(expected, reader.GetValue(0, SQLiteColumnType.Integer, typeof(long)));
    }

    [Fact]
    public void RealValueOnACommentOnlyReaderMatchesOutOfRangeBehavior()
    {
        using TestDatabase db = new();

        object? expected = OutOfRangeValue(db, SQLiteColumnType.Real, typeof(double));

        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        Assert.Equal(expected, reader.GetValue(0, SQLiteColumnType.Real, typeof(double)));
    }

    [Fact]
    public void BlobValueOnACommentOnlyReaderMatchesOutOfRangeBehavior()
    {
        using TestDatabase db = new();

        object? expected = OutOfRangeValue(db, SQLiteColumnType.Blob, typeof(byte[]));

        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        Assert.Equal(expected, reader.GetValue(0, SQLiteColumnType.Blob, typeof(byte[])));
    }

    [Fact]
    public void IntegerValueOnASemicolonOnlyReaderMatchesOutOfRangeBehavior()
    {
        using TestDatabase db = new();

        object? expected = OutOfRangeValue(db, SQLiteColumnType.Integer, typeof(long));

        using SQLiteDataReader reader = db.CreateCommand(" ; ; ", []).ExecuteReader();

        Assert.Equal(expected, reader.GetValue(0, SQLiteColumnType.Integer, typeof(long)));
    }

    private static object? OutOfRangeValue(TestDatabase db, SQLiteColumnType columnType, Type type)
    {
        using SQLiteDataReader reader = db.CreateCommand("SELECT 1 AS \"A\"", []).ExecuteReader();
        Assert.True(reader.Read());
        return reader.GetValue(5, columnType, type);
    }
}
