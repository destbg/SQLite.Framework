using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StatementlessReaderMemberTests
{
    [Fact]
    public void GetNameOnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetName(0));
    }

    [Fact]
    public void IsDBNullOnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.IsDBNull(0));
    }

    [Fact]
    public void GetValueOnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetValue(0, SQLiteColumnType.Integer, typeof(long)));
    }

    [Fact]
    public void GetInt32OnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetInt32(0));
    }

    [Fact]
    public void GetInt64OnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetInt64(0));
    }

    [Fact]
    public void GetInt16OnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetInt16(0));
    }

    [Fact]
    public void GetUInt16OnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetUInt16(0));
    }

    [Fact]
    public void GetByteValueOnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetByteValue(0));
    }

    [Fact]
    public void GetSByteValueOnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetSByteValue(0));
    }

    [Fact]
    public void GetUInt32OnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetUInt32(0));
    }

    [Fact]
    public void GetUInt64OnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetUInt64(0));
    }

    [Fact]
    public void GetDoubleOnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetDouble(0));
    }

    [Fact]
    public void GetSingleOnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetSingle(0));
    }

    [Fact]
    public void GetBooleanOnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetBoolean(0));
    }

    [Fact]
    public void GetStringOnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetString(0));
    }

    [Fact]
    public void GetBlobSpanOnACommentOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("-- nothing here", []).ExecuteReader();

        AssertNotAnArgumentNull(() => { _ = reader.GetBlobSpan(0).Length; });
    }

    [Fact]
    public void GetNameOnASemicolonOnlyReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand(" ; ; ", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetName(0));
    }

    [Fact]
    public void GetInt64OnAnEmptySqlReaderDoesNotBlameTheCaller()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("", []).ExecuteReader();

        AssertNotAnArgumentNull(() => reader.GetInt64(0));
    }

    private static void AssertNotAnArgumentNull(Action read)
    {
        Exception? exception = Record.Exception(read);

        Assert.False(exception is ArgumentNullException, exception?.GetType().FullName ?? "no exception");
    }
}
