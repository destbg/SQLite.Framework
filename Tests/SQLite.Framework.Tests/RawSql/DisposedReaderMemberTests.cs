using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DisposedMemberItem")]
public class DisposedMemberItem
{
    [Key]
    public int Id { get; set; }
}

public class DisposedReaderMemberTests
{
    private static SQLiteDataReader DisposedReader(TestDatabase db)
    {
        db.Table<DisposedMemberItem>().Schema.CreateTable();
        db.Table<DisposedMemberItem>().Add(new DisposedMemberItem { Id = 1 });

        SQLiteDataReader reader = db.CreateCommand("SELECT Id FROM DisposedMemberItem", []).ExecuteReader();
        Assert.True(reader.Read());
        reader.Dispose();
        return reader;
    }

    [Fact]
    public void ReadAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.Read());
    }

    [Fact]
    public void FieldCountAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.FieldCount);
    }

    [Fact]
    public void GetNameAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetName(0));
    }

    [Fact]
    public void GetColumnTypeAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetColumnType(0));
    }

    [Fact]
    public void GetValueAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetValue(0, SQLiteColumnType.Integer, typeof(long)));
    }

    [Fact]
    public void IsDBNullAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.IsDBNull(0));
    }

    [Fact]
    public void GetInt32AfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetInt32(0));
    }

    [Fact]
    public void GetInt64AfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetInt64(0));
    }

    [Fact]
    public void GetInt16AfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetInt16(0));
    }

    [Fact]
    public void GetUInt16AfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetUInt16(0));
    }

    [Fact]
    public void GetByteValueAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetByteValue(0));
    }

    [Fact]
    public void GetSByteValueAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetSByteValue(0));
    }

    [Fact]
    public void GetUInt32AfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetUInt32(0));
    }

    [Fact]
    public void GetUInt64AfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetUInt64(0));
    }

    [Fact]
    public void GetDoubleAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetDouble(0));
    }

    [Fact]
    public void GetSingleAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetSingle(0));
    }

    [Fact]
    public void GetBooleanAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetBoolean(0));
    }

    [Fact]
    public void GetStringAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetString(0));
    }

    [Fact]
    public void GetBlobSpanAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetBlobSpan(0).Length);
    }

    [Fact]
    public void GetDateTimeValueAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetDateTimeValue(0));
    }

    [Fact]
    public void GetDateTimeOffsetValueAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetDateTimeOffsetValue(0));
    }

    [Fact]
    public void GetTimeSpanValueAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetTimeSpanValue(0));
    }

    [Fact]
    public void GetDateOnlyValueAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetDateOnlyValue(0));
    }

    [Fact]
    public void GetTimeOnlyValueAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetTimeOnlyValue(0));
    }

    [Fact]
    public void GetGuidValueAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetGuidValue(0));
    }

    [Fact]
    public void GetDecimalValueAfterDisposeThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteDataReader reader = DisposedReader(db);

        Assert.Throws<ObjectDisposedException>(() => reader.GetDecimalValue(0));
    }
}
