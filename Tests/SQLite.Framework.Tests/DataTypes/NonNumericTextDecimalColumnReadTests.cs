using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NonNumericTextDecimalColumnReadTests
{
    [Fact]
    public void AnEmptyStoredTextReadsIntoADecimalTheSameWayItReadsIntoADouble()
    {
        using TestDatabase db = new(null, nameof(AnEmptyStoredTextReadsIntoADecimalTheSameWayItReadsIntoADouble));

        double viaReader = ReadDouble(db, "SELECT ''");
        decimal viaScalar = db.ExecuteScalar<decimal>("SELECT ''");

        Assert.Equal(0d, viaReader);
        Assert.Equal((decimal)viaReader, viaScalar);
    }

    [Fact]
    public void AStoredTextWithATrailingSuffixReadsIntoADecimalTheSameWayItReadsIntoADouble()
    {
        using TestDatabase db = new(null, nameof(AStoredTextWithATrailingSuffixReadsIntoADecimalTheSameWayItReadsIntoADouble));

        double viaReader = ReadDouble(db, "SELECT '42.5abc'");
        decimal viaScalar = db.ExecuteScalar<decimal>("SELECT '42.5abc'");

        Assert.Equal(42.5d, viaReader);
        Assert.Equal((decimal)viaReader, viaScalar);
    }

    private static double ReadDouble(TestDatabase db, string sql)
    {
        using SQLiteDataReader reader = db.CreateCommand(sql, []).ExecuteReader();
        Assert.True(reader.Read());
        return reader.GetDouble(0);
    }
}
