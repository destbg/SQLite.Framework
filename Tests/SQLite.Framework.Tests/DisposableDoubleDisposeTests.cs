using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DoubleDisposeItem")]
public class DoubleDisposeItem
{
    [Key]
    public int Id { get; set; }
}

public class DisposableDoubleDisposeTests
{
    [Fact]
    public void ReadLockLeaseSecondDisposeDoesNothing()
    {
        using TestDatabase db = new();
        db.OpenConnection();

        IDisposable lease = db.ReadLock();
        lease.Dispose();
        lease.Dispose();

        Assert.Equal(1, db.ExecuteScalar<long>("SELECT 1"));
    }

    [Fact]
    public void NestedLockLeaseSecondDisposeDoesNothing()
    {
        using TestDatabase db = new();
        db.OpenConnection();

        using (db.Lock())
        {
            IDisposable nested = db.Lock();
            nested.Dispose();
            nested.Dispose();

            Assert.Equal(1, db.ExecuteScalar<long>("SELECT 1"));
        }

        using (db.Lock())
        {
            Assert.Equal(2, db.ExecuteScalar<long>("SELECT 2"));
        }
    }

    [Fact]
    public void ReaderSecondDisposeDoesNothing()
    {
        using TestDatabase db = new();
        db.Table<DoubleDisposeItem>().Schema.CreateTable();
        db.Table<DoubleDisposeItem>().Add(new DoubleDisposeItem { Id = 1 });

        SQLiteDataReader reader = db.CreateCommand("SELECT Id FROM DoubleDisposeItem", []).ExecuteReader();
        Assert.True(reader.Read());
        reader.Dispose();
        reader.Dispose();

        using SQLiteDataReader next = db.CreateCommand("SELECT Id FROM DoubleDisposeItem", []).ExecuteReader();
        Assert.True(next.Read());
        Assert.Equal(1, next.GetInt32(0));
    }

    [Fact]
    public void TransactionSecondDisposeDoesNothing()
    {
        using TestDatabase db = new();
        db.Table<DoubleDisposeItem>().Schema.CreateTable();

        SQLiteTransaction transaction = db.BeginTransaction();
        db.Table<DoubleDisposeItem>().Add(new DoubleDisposeItem { Id = 5 });
        transaction.Dispose();
        transaction.Dispose();

        Assert.Equal(0, db.ExecuteScalar<long>("SELECT COUNT(*) FROM DoubleDisposeItem"));

        using (db.Lock())
        {
            Assert.Equal(1, db.ExecuteScalar<long>("SELECT 1"));
        }
    }

    [Fact]
    public void BlobStreamSecondDisposeDoesNothing()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = [1, 2, 3] });

        SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);
        stream.Dispose();
        stream.Dispose();

        using (db.Lock())
        {
            Assert.Equal(1, db.ExecuteScalar<long>("SELECT 1"));
        }
    }

    [Fact]
    public void DatabaseSecondDisposeDoesNothing()
    {
        TestDatabase db = new();
        db.OpenConnection();

        db.Dispose();
        db.Dispose();

        Assert.Throws<ObjectDisposedException>(() => db.OpenConnection());
    }
}
