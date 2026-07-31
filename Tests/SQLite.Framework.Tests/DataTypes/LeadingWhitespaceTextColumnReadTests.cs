using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26eLeadingSpaceRows")]
public class H26eLeadingSpaceRow
{
    [Key]
    public int Id { get; set; }

    public long Amount { get; set; }

    public long? MaybeAmount { get; set; }
}

public class LeadingWhitespaceTextColumnReadTests
{
    [Fact]
    public void AStoredTextWithALeadingSpaceReadsIntoALongTheSameWayTheReaderDoes()
    {
        using TestDatabase db = new(null, nameof(AStoredTextWithALeadingSpaceReadsIntoALongTheSameWayTheReaderDoes));

        long viaReader = ReadInt64(db, "SELECT ' 42abc'");
        long viaScalar = db.ExecuteScalar<long>("SELECT ' 42abc'");

        Assert.Equal(42L, viaReader);
        Assert.Equal(viaReader, viaScalar);
    }

    [Fact]
    public void AStoredTextWithALeadingSpaceReadsIntoADoubleTheSameWayTheReaderDoes()
    {
        using TestDatabase db = new(null, nameof(AStoredTextWithALeadingSpaceReadsIntoADoubleTheSameWayTheReaderDoes));

        double viaReader = ReadDouble(db, "SELECT ' 1.5abc'");
        double viaScalar = db.ExecuteScalar<double>("SELECT ' 1.5abc'");

        Assert.Equal(1.5d, viaReader);
        Assert.Equal(viaReader, viaScalar);
    }

    [Fact]
    public void AStoredTextWithALeadingSpaceReadsIntoABooleanTheSameWayTheReaderDoes()
    {
        using TestDatabase db = new(null, nameof(AStoredTextWithALeadingSpaceReadsIntoABooleanTheSameWayTheReaderDoes));

        bool viaReader = ReadBoolean(db, "SELECT ' 1abc'");
        bool viaScalar = db.ExecuteScalar<bool>("SELECT ' 1abc'");

        Assert.True(viaReader);
        Assert.Equal(viaReader, viaScalar);
    }

    [Fact]
    public void ANullableLongColumnReadsALeadingSpaceTextTheSameWayAPlainLongColumnDoes()
    {
        using TestDatabase db = new(null, nameof(ANullableLongColumnReadsALeadingSpaceTextTheSameWayAPlainLongColumnDoes));
        db.Execute("CREATE TABLE \"H26eLeadingSpaceRows\" (\"Id\" INTEGER PRIMARY KEY, \"Amount\" TEXT, \"MaybeAmount\" TEXT)");
        db.Execute("INSERT INTO \"H26eLeadingSpaceRows\" (\"Id\", \"Amount\", \"MaybeAmount\") VALUES (1, ' 42abc', ' 42abc')");

        long viaReader = ReadInt64(db, "SELECT \"Amount\" FROM \"H26eLeadingSpaceRows\"");
        H26eLeadingSpaceRow row = db.Table<H26eLeadingSpaceRow>().Single();

        Assert.Equal(42L, viaReader);
        Assert.Equal(viaReader, row.Amount);
        Assert.Equal<long?>(viaReader, row.MaybeAmount);
    }

    private static long ReadInt64(TestDatabase db, string sql)
    {
        using SQLiteDataReader reader = db.CreateCommand(sql, []).ExecuteReader();
        Assert.True(reader.Read());
        return reader.GetInt64(0);
    }

    private static double ReadDouble(TestDatabase db, string sql)
    {
        using SQLiteDataReader reader = db.CreateCommand(sql, []).ExecuteReader();
        Assert.True(reader.Read());
        return reader.GetDouble(0);
    }

    private static bool ReadBoolean(TestDatabase db, string sql)
    {
        using SQLiteDataReader reader = db.CreateCommand(sql, []).ExecuteReader();
        Assert.True(reader.Read());
        return reader.GetBoolean(0);
    }
}
