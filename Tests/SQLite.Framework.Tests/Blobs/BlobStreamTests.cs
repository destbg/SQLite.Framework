using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BlobStreamTests
{
    [Fact]
    public void OpenBlobStream_ReadsExistingBlob()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();

        byte[] payload = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = payload });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);

        Assert.True(stream.CanRead);
        Assert.False(stream.CanWrite);
        Assert.True(stream.CanSeek);
        Assert.Equal(payload.Length, stream.Length);

        byte[] buffer = new byte[payload.Length];
        int read = stream.Read(buffer, 0, buffer.Length);

        Assert.Equal(payload.Length, read);
        Assert.Equal(payload, buffer);
        Assert.Equal(payload.Length, stream.Position);
    }

    [Fact]
    public void OpenBlobStream_PartialReadAndSeek()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = [10, 20, 30, 40, 50] });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);

        byte[] head = new byte[2];
        Assert.Equal(2, stream.Read(head));
        Assert.Equal(new byte[] { 10, 20 }, head);

        stream.Seek(-1, SeekOrigin.End);
        byte[] tail = new byte[1];
        Assert.Equal(1, stream.Read(tail));
        Assert.Equal(50, tail[0]);

        stream.Position = 0;
        byte[] all = new byte[5];
        Assert.Equal(5, stream.Read(all));
        Assert.Equal(new byte[] { 10, 20, 30, 40, 50 }, all);
    }

    [Fact]
    public void OpenBlobStream_WritesExistingBlob()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = new byte[8] });

        using (SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue, writable: true))
        {
            Assert.True(stream.CanWrite);
            stream.Write([0xDE, 0xAD, 0xBE, 0xEF], 0, 4);
            stream.Write([0x01, 0x02, 0x03, 0x04]);
        }

        NumericType row = db.Table<NumericType>().First(n => n.Id == 1);
        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04 }, row.BlobValue);
    }

    [Fact]
    public void OpenBlobStream_WriteExceedingLengthThrows()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = new byte[4] });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue, writable: true);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            stream.Write([1, 2, 3, 4, 5]));
        Assert.Contains("exceed the blob size", ex.Message);
    }

    [Fact]
    public void OpenBlobStream_WriteOnReadOnlyThrows()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = new byte[4] });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);

        Assert.Throws<NotSupportedException>(() => stream.Write([1, 2], 0, 2));
    }

    [Fact]
    public void OpenBlobStream_SetLengthThrows()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = new byte[4] });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue, writable: true);

        Assert.Throws<NotSupportedException>(() => stream.SetLength(8));
    }

    [Fact]
    public void OpenBlobStream_DisposedStreamRejectsOperations()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = new byte[4] });

        SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);
        stream.Dispose();
        stream.Dispose();

        Assert.False(stream.CanRead);
        Assert.False(stream.CanSeek);
        Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[4]));
    }

    [Fact]
    public void OpenBlobStream_MissingRowThrows()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();

        SQLiteException ex = Assert.Throws<SQLiteException>(() =>
            db.OpenBlobStream<NumericType>(99, n => n.BlobValue));
        Assert.Equal(SQLiteResult.Error, ex.Result);
    }

    [Fact]
    public void OpenBlobStream_NonBlobColumnThrows()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 42 });

        SQLiteException ex = Assert.Throws<SQLiteException>(() =>
            db.OpenBlobStream("NumericTypes", "Id", 1));
        Assert.Equal(SQLiteResult.Error, ex.Result);
    }

    [Fact]
    public void OpenBlobStream_InvalidSelectorThrows()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            db.OpenBlobStream<NumericType>(1, _ => new byte[4]));
        Assert.Contains("property access expression", ex.Message);
    }

    [Fact]
    public void OpenBlobStream_UnmappedPropertyThrows()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            db.OpenBlobStream<UnmappedBlobEntity>(1, n => n.NotMapped));
        Assert.Contains("not mapped", ex.Message);
    }

    [Fact]
    public async Task OpenBlobStreamAsync_ReadsBlob()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = [1, 2, 3, 4] });

        await using SQLiteBlobStream stream = await db.OpenBlobStreamAsync<NumericType>(1, n => n.BlobValue);
        byte[] buffer = new byte[4];
        int read = await stream.ReadAsync(buffer);
        Assert.Equal(4, read);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, buffer);
    }

    [Fact]
    public async Task OpenBlobStreamAsync_ByTableAndColumn_Writes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = new byte[4] });

        await using (SQLiteBlobStream stream = await db.OpenBlobStreamAsync("NumericTypes", "BlobValue", 1, writable: true))
        {
            await stream.WriteAsync(new byte[] { 9, 8, 7, 6 });
        }

        Assert.Equal(new byte[] { 9, 8, 7, 6 }, db.Table<NumericType>().First(n => n.Id == 1).BlobValue);
    }

    [Fact]
    public void OpenBlobStream_Reopen_MovesToOtherRow()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = [1, 2, 3, 4] });
        db.Table<NumericType>().Add(new NumericType { Id = 2, BlobValue = [9, 8, 7, 6] });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);
        byte[] firstRow = new byte[4];
        int firstRead = stream.Read(firstRow);
        Assert.Equal(4, firstRead);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, firstRow);

        stream.Reopen(2);
        byte[] secondRow = new byte[4];
        int secondRead = stream.Read(secondRow);
        Assert.Equal(4, secondRead);
        Assert.Equal(new byte[] { 9, 8, 7, 6 }, secondRow);
    }

    [Fact]
    public void OpenBlobStream_SeekOutOfRangeThrows()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = new byte[4] });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);

        Assert.Throws<IOException>(() => stream.Seek(-1, SeekOrigin.Begin));
        Assert.Throws<IOException>(() => stream.Seek(5, SeekOrigin.Begin));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Seek(0, (SeekOrigin)99));
    }

    [Fact]
    public void OpenBlobStream_ReadByteArrayOffsetCountOverflow()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = new byte[4] });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);

        byte[] buffer = new byte[4];
        Assert.Throws<ArgumentException>(() => stream.Read(buffer, 2, 3));
    }

    [Fact]
    public void OpenBlobStream_WriteByteArrayOffsetCountOverflow()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = new byte[4] });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue, writable: true);

        byte[] buffer = new byte[4];
        Assert.Throws<ArgumentException>(() => stream.Write(buffer, 2, 3));
    }

    [Fact]
    public void OpenBlobStream_SeekFromCurrent()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = [10, 20, 30, 40, 50] });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);
        stream.Position = 1;
        long after = stream.Seek(2, SeekOrigin.Current);

        Assert.Equal(3, after);
        Assert.Equal(3, stream.Position);
    }

    [Fact]
    public void OpenBlobStream_FlushIsNoOp()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = new byte[4] });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue, writable: true);
        stream.Flush();
    }

    [Fact]
    public void OpenBlobStream_ReopenToMissingRowThrows()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = new byte[4] });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);
        Assert.Throws<SQLiteException>(() => stream.Reopen(999));
    }

    [Fact]
    public void OpenBlobStream_PositionPastEndReturnsZero()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = [1, 2, 3, 4] });

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);
        stream.Position = 4;

        byte[] buffer = new byte[4];
        Assert.Equal(0, stream.Read(buffer));
    }
}

[System.ComponentModel.DataAnnotations.Schema.Table("UnmappedBlobEntities")]
file sealed class UnmappedBlobEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public byte[]? NotMapped { get; set; }
}
