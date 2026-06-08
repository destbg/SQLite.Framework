using System;
using System.IO;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BlobStreamDisposedMemberTests
{
    private static SQLiteBlobStream DisposedStream(TestDatabase db)
    {
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = [1, 2, 3, 4] });
        SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);
        stream.Dispose();
        return stream;
    }

    [Fact]
    public void Seek_AfterDispose_ThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteBlobStream stream = DisposedStream(db);

        Assert.Throws<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.Begin));
    }

    [Fact]
    public void LengthGet_AfterDispose_ThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteBlobStream stream = DisposedStream(db);

        Assert.Throws<ObjectDisposedException>(() => stream.Length);
    }

    [Fact]
    public void PositionGet_AfterDispose_ThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteBlobStream stream = DisposedStream(db);

        Assert.Throws<ObjectDisposedException>(() => stream.Position);
    }

    [Fact]
    public void PositionSet_AfterDispose_ThrowsObjectDisposed()
    {
        using TestDatabase db = new();
        SQLiteBlobStream stream = DisposedStream(db);

        Assert.Throws<ObjectDisposedException>(() => stream.Position = 0);
    }
}
