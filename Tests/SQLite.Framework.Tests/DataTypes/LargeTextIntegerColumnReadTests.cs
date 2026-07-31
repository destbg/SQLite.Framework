using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26eLargeTextIntegerRows")]
public class H26eLargeTextIntegerRow
{
    [Key]
    public int Id { get; set; }

    public long Amount { get; set; }

    public long? MaybeAmount { get; set; }
}

public class LargeTextIntegerColumnReadTests
{
    [Fact]
    public void AStoredTextIntegerBeyondDoublePrecisionReadsIntoALongWithoutRounding()
    {
        using TestDatabase db = new(null, nameof(AStoredTextIntegerBeyondDoublePrecisionReadsIntoALongWithoutRounding));

        long viaReader = ReadInt64(db, "SELECT '9007199254740993'");
        long viaScalar = db.ExecuteScalar<long>("SELECT '9007199254740993'");

        Assert.Equal(9007199254740993L, viaReader);
        Assert.Equal(viaReader, viaScalar);
    }

    [Fact]
    public void ANullableLongColumnReadsALargeTextIntegerTheSameWayAPlainLongColumnDoes()
    {
        using TestDatabase db = new(null, nameof(ANullableLongColumnReadsALargeTextIntegerTheSameWayAPlainLongColumnDoes));
        db.Execute("CREATE TABLE \"H26eLargeTextIntegerRows\" (\"Id\" INTEGER PRIMARY KEY, \"Amount\" TEXT, \"MaybeAmount\" TEXT)");
        db.Execute(
            "INSERT INTO \"H26eLargeTextIntegerRows\" (\"Id\", \"Amount\", \"MaybeAmount\") "
            + "VALUES (1, '9007199254740993', '9007199254740993')");

        long viaReader = ReadInt64(db, "SELECT \"Amount\" FROM \"H26eLargeTextIntegerRows\"");
        H26eLargeTextIntegerRow row = db.Table<H26eLargeTextIntegerRow>().Single();

        Assert.Equal(9007199254740993L, viaReader);
        Assert.Equal(viaReader, row.Amount);
        Assert.Equal<long?>(viaReader, row.MaybeAmount);
    }

    private static long ReadInt64(TestDatabase db, string sql)
    {
        using SQLiteDataReader reader = db.CreateCommand(sql, []).ExecuteReader();
        Assert.True(reader.Read());
        return reader.GetInt64(0);
    }
}
