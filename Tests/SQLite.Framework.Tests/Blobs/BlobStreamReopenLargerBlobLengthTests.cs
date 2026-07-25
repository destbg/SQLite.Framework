using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BlobStreamReopenLargerBlobLengthTests
{
    [Fact]
    public void Reopen_ToLargerBlob_ReportsFullLengthAndReadsAllBytes()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NumericType>();

        byte[] smallRow = [1, 2, 3];
        byte[] largeRow = [11, 22, 33, 44, 55, 66];
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = smallRow });
        db.Table<NumericType>().Add(new NumericType { Id = 2, BlobValue = largeRow });

        byte[] oracleBytes = db.Table<NumericType>().First(n => n.Id == 2).BlobValue!;
        long oracleLength = oracleBytes.Length;

        using SQLiteBlobStream stream = db.OpenBlobStream<NumericType>(1, n => n.BlobValue);
        stream.Reopen(2);

        Assert.Equal(oracleLength, stream.Length);
        Assert.Equal(6L, stream.Length);

        byte[] buffer = new byte[oracleBytes.Length];
        int read = stream.Read(buffer, 0, buffer.Length);

        Assert.Equal(oracleBytes.Length, read);
        Assert.Equal(oracleBytes, buffer);
        Assert.Equal(new byte[] { 11, 22, 33, 44, 55, 66 }, buffer);
    }
}