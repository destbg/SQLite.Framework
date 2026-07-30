using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25hPlainReads")]
public class H25hPlainRead
{
    [Key]
    public int Id { get; set; }

    public long Big { get; set; }

    public bool Flag { get; set; }

    public double Rate { get; set; }

    public ulong Amount { get; set; }
}

[Table("H25hMaybeReads")]
public class H25hMaybeRead
{
    [Key]
    public int Id { get; set; }

    public long? Big { get; set; }

    public bool? Flag { get; set; }

    public double? Rate { get; set; }

    public ulong? Amount { get; set; }
}

public class NullableColumnReadCoercionAgreementTests
{
    [Fact]
    public void NullableLongReadsAStoredRealBeyondTheLongRangeTheSameWayALongDoes()
    {
        using TestDatabase db = Setup(nameof(NullableLongReadsAStoredRealBeyondTheLongRangeTheSameWayALongDoes), "9.3e18", "1", "1.5", "1");

        long viaReader = ReadInt64(db, "Big");
        long? viaPlainColumn = db.Table<H25hPlainRead>().Single().Big;
        long? viaNullableColumn = db.Table<H25hMaybeRead>().Single().Big;

        Assert.Equal<long?>(viaReader, viaPlainColumn);
        Assert.Equal<long?>(viaReader, viaNullableColumn);
    }

    [Fact]
    public void NullableBooleanReadsAStoredTextValueTheSameWayABooleanDoes()
    {
        using TestDatabase db = Setup(nameof(NullableBooleanReadsAStoredTextValueTheSameWayABooleanDoes), "1", "'yes'", "1.5", "1");

        bool viaReader = ReadBoolean(db, "Flag");
        bool? viaPlainColumn = db.Table<H25hPlainRead>().Single().Flag;
        bool? viaNullableColumn = db.Table<H25hMaybeRead>().Single().Flag;

        Assert.Equal<bool?>(viaReader, viaPlainColumn);
        Assert.Equal<bool?>(viaReader, viaNullableColumn);
    }

    [Fact]
    public void NullableDoubleReadsAStoredTextValueTheSameWayADoubleDoes()
    {
        using TestDatabase db = Setup(nameof(NullableDoubleReadsAStoredTextValueTheSameWayADoubleDoes), "1", "1", "'abc'", "1");

        double viaReader = ReadDouble(db, "Rate");
        double? viaPlainColumn = db.Table<H25hPlainRead>().Single().Rate;
        double? viaNullableColumn = db.Table<H25hMaybeRead>().Single().Rate;

        Assert.Equal<double?>(viaReader, viaPlainColumn);
        Assert.Equal<double?>(viaReader, viaNullableColumn);
    }

    [Fact]
    public void NullableLongReadsAStoredEmptyTextTheSameWayALongDoes()
    {
        using TestDatabase db = Setup(nameof(NullableLongReadsAStoredEmptyTextTheSameWayALongDoes), "''", "1", "1.5", "1");

        long viaReader = ReadInt64(db, "Big");
        long? viaPlainColumn = db.Table<H25hPlainRead>().Single().Big;
        long? viaNullableColumn = db.Table<H25hMaybeRead>().Single().Big;

        Assert.Equal(0L, viaReader);
        Assert.Equal<long?>(viaReader, viaPlainColumn);
        Assert.Equal<long?>(viaReader, viaNullableColumn);
    }

    [Fact]
    public void AnUnsignedLongReadsAStoredRealBelowTheLongRangeTheSameWayTheReaderDoes()
    {
        using TestDatabase db = Setup(nameof(AnUnsignedLongReadsAStoredRealBelowTheLongRangeTheSameWayTheReaderDoes), "1", "1", "1.5", "-9.3e18");

        ulong viaReader = ReadUInt64(db, "Amount");
        ulong? viaPlainColumn = db.Table<H25hPlainRead>().Single().Amount;
        ulong? viaNullableColumn = db.Table<H25hMaybeRead>().Single().Amount;

        Assert.Equal<ulong?>(viaReader, viaPlainColumn);
        Assert.Equal<ulong?>(viaReader, viaNullableColumn);
    }

    private static long ReadInt64(TestDatabase db, string column)
    {
        using SQLiteDataReader reader = OpenReader(db, column);
        Assert.True(reader.Read());
        return reader.GetInt64(0);
    }

    private static ulong ReadUInt64(TestDatabase db, string column)
    {
        using SQLiteDataReader reader = OpenReader(db, column);
        Assert.True(reader.Read());
        return reader.GetUInt64(0);
    }

    private static bool ReadBoolean(TestDatabase db, string column)
    {
        using SQLiteDataReader reader = OpenReader(db, column);
        Assert.True(reader.Read());
        return reader.GetBoolean(0);
    }

    private static double ReadDouble(TestDatabase db, string column)
    {
        using SQLiteDataReader reader = OpenReader(db, column);
        Assert.True(reader.Read());
        return reader.GetDouble(0);
    }

    private static SQLiteDataReader OpenReader(TestDatabase db, string column)
    {
        return db.CreateCommand($"SELECT \"{column}\" FROM \"H25hMaybeReads\"", []).ExecuteReader();
    }

    private static TestDatabase Setup(string methodName, string big, string flag, string rate, string amount)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25hPlainRead>().Schema.CreateTable();
        db.Table<H25hMaybeRead>().Schema.CreateTable();
        db.Execute(
            "INSERT INTO \"H25hPlainReads\" (\"Id\", \"Big\", \"Flag\", \"Rate\", \"Amount\") "
            + $"VALUES (1, {big}, {flag}, {rate}, {amount})");
        db.Execute(
            "INSERT INTO \"H25hMaybeReads\" (\"Id\", \"Big\", \"Flag\", \"Rate\", \"Amount\") "
            + $"VALUES (1, {big}, {flag}, {rate}, {amount})");
        return db;
    }
}
