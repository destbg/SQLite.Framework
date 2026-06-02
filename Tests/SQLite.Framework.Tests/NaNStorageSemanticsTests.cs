using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NaNNullableDoubleRows")]
internal sealed class NaNNullableDoubleRow
{
    [Key]
    public int Id { get; set; }

    public double? Value { get; set; }
}

[Table("NaNDoubleRows")]
internal sealed class NaNDoubleRow
{
    [Key]
    public int Id { get; set; }

    public double Value { get; set; }
}

[Table("NaNFloatRows")]
internal sealed class NaNFloatRow
{
    [Key]
    public int Id { get; set; }

    public float Value { get; set; }
}

public class NaNStorageSemanticsTests
{
    [Fact]
    public void NullableNaN_IsStoredAsSqlNull()
    {
        using TestDatabase db = new();
        db.Table<NaNNullableDoubleRow>().Schema.CreateTable();
        db.Table<NaNNullableDoubleRow>().Add(new NaNNullableDoubleRow { Id = 1, Value = double.NaN });

        string storedType = db.ExecuteScalar<string>(
            "SELECT typeof(\"Value\") FROM \"NaNNullableDoubleRows\" WHERE \"Id\" = 1")!;
        double? roundTripped = db.Table<NaNNullableDoubleRow>().First().Value;

        Assert.Equal("null", storedType);
        Assert.Null(roundTripped);
    }

    [Fact]
    public void NonNullableDoubleNaN_FailsNotNullConstraint()
    {
        using TestDatabase db = new();
        db.Table<NaNDoubleRow>().Schema.CreateTable();

        Assert.Throws<SQLiteException>(() =>
            db.Table<NaNDoubleRow>().Add(new NaNDoubleRow { Id = 1, Value = double.NaN }));
    }

    [Fact]
    public void NonNullableFloatNaN_FailsNotNullConstraint()
    {
        using TestDatabase db = new();
        db.Table<NaNFloatRow>().Schema.CreateTable();

        Assert.Throws<SQLiteException>(() =>
            db.Table<NaNFloatRow>().Add(new NaNFloatRow { Id = 1, Value = float.NaN }));
    }
}
