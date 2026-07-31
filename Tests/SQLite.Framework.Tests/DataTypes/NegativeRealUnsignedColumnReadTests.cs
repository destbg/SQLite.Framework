using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26eUnsignedFractionalRows")]
public class H26eUnsignedFractionalRow
{
    [Key]
    public int Id { get; set; }

    public uint UnsignedWhole { get; set; }

    public ulong UnsignedBig { get; set; }
}

public class NegativeRealUnsignedColumnReadTests
{
    [Fact]
    public void ANegativeStoredRealReadsIntoAnUnsignedIntTheSameWayTheReaderDoes()
    {
        using TestDatabase db = new(null, nameof(ANegativeStoredRealReadsIntoAnUnsignedIntTheSameWayTheReaderDoes));

        uint viaReader = ReadUInt32(db, "SELECT -1.5");
        uint viaScalar = db.ExecuteScalar<uint>("SELECT -1.5");

        Assert.Equal(uint.MaxValue, viaReader);
        Assert.Equal(viaReader, viaScalar);
    }

    [Fact]
    public void ANegativeStoredRealReadsIntoAnUnsignedIntColumnTheSameWayItReadsIntoAnUnsignedLongColumn()
    {
        using TestDatabase db = new(null, nameof(ANegativeStoredRealReadsIntoAnUnsignedIntColumnTheSameWayItReadsIntoAnUnsignedLongColumn));
        db.Table<H26eUnsignedFractionalRow>().Schema.CreateTable();
        db.Execute(
            "INSERT INTO \"H26eUnsignedFractionalRows\" (\"Id\", \"UnsignedWhole\", \"UnsignedBig\") "
            + "VALUES (1, -1.5, -1.5)");

        uint viaWholeReader = ReadUInt32(db, "SELECT \"UnsignedWhole\" FROM \"H26eUnsignedFractionalRows\"");
        ulong viaBigReader = ReadUInt64(db, "SELECT \"UnsignedBig\" FROM \"H26eUnsignedFractionalRows\"");

        H26eUnsignedFractionalRow row = db.Table<H26eUnsignedFractionalRow>().Single();

        Assert.Equal(viaBigReader, row.UnsignedBig);
        Assert.Equal(viaWholeReader, row.UnsignedWhole);
    }

    private static uint ReadUInt32(TestDatabase db, string sql)
    {
        using SQLiteDataReader reader = db.CreateCommand(sql, []).ExecuteReader();
        Assert.True(reader.Read());
        return reader.GetUInt32(0);
    }

    private static ulong ReadUInt64(TestDatabase db, string sql)
    {
        using SQLiteDataReader reader = db.CreateCommand(sql, []).ExecuteReader();
        Assert.True(reader.Read());
        return reader.GetUInt64(0);
    }
}
