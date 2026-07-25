using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DataReaderBlobSpanTests
{
    [Fact]
    public void GetBlobSpan_ReturnsBlobBytesWithoutCopyPerRow()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, BlobValue = [1, 2, 3, 4] });
        db.Table<NumericType>().Add(new NumericType { Id = 2, BlobValue = [] });
        db.Table<NumericType>().Add(new NumericType { Id = 3, BlobValue = null });

        using SQLiteDataReader reader = db
            .CreateCommand("SELECT \"BlobValue\" FROM \"NumericTypes\" ORDER BY \"Id\"", [])
            .ExecuteReader();

        Assert.True(reader.Read());
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, reader.GetBlobSpan(0).ToArray());

        Assert.True(reader.Read());
        Assert.Equal(0, reader.GetBlobSpan(0).Length);
        Assert.False(reader.IsDBNull(0));

        Assert.True(reader.Read());
        Assert.Equal(0, reader.GetBlobSpan(0).Length);
        Assert.True(reader.IsDBNull(0));

        Assert.False(reader.Read());
    }
}
