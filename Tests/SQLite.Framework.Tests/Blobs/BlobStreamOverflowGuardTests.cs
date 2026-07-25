using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BlobStreamOverflowGuardTests
{
    private static TestDatabase SeedBlob()
    {
        TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = [1, 2, 3, 4] });
        return db;
    }

    [Fact]
    public void Read_OffsetPlusCountOverflowsInt_ThrowsArgumentException()
    {
        using TestDatabase db = SeedBlob();
        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);

        byte[] buffer = new byte[4];
        Assert.Throws<ArgumentException>(() => stream.Read(buffer, int.MaxValue, 2));
    }

    [Fact]
    public void Write_OffsetPlusCountOverflowsInt_ThrowsArgumentException()
    {
        using TestDatabase db = SeedBlob();
        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue, writable: true);

        byte[] buffer = new byte[4];
        Assert.Throws<ArgumentException>(() => stream.Write(buffer, int.MaxValue, 2));
    }
}
