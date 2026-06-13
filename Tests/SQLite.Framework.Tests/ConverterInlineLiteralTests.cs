using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ConverterInlineLiteralTests
{
    private static TestDatabase Create()
    {
        return new TestDatabase(builder =>
        {
            builder.AddTypeConverter<decimal>(new CentsConverter());
            builder.AddTypeConverter<PointTally>(new PointTallyConverter());
        });
    }

    [Fact]
    public void DefaultValueColumnRoundTripsThroughConverter()
    {
        using TestDatabase db = Create();
        db.Table<CentsAmountRow>().Schema.CreateTable();

        db.Table<CentsAmountRow>().Add(new CentsAmountRow { Id = 1 });

        decimal stored = db.Table<CentsAmountRow>().First().Amount;
        Assert.Equal(9.99m, stored);
    }

    [Fact]
    public void MigrateSetValueRoundTripsThroughConverter()
    {
        using TestDatabase db = Create();
        db.CreateCommand("CREATE TABLE \"CentsAmountRow\" (\"Id\" INTEGER PRIMARY KEY)", []).ExecuteNonQuery();
        db.CreateCommand("INSERT INTO \"CentsAmountRow\" (\"Id\") VALUES (1)", []).ExecuteNonQuery();

        db.Schema.Migrate<CentsAmountRow>(m => m.Set(e => e.Amount, 4.5m));

        decimal stored = db.Table<CentsAmountRow>().First().Amount;
        Assert.Equal(4.5m, stored);
    }

    [Fact]
    public void MigrateSetValueUsesConverterForCustomType()
    {
        using TestDatabase db = Create();
        db.CreateCommand("CREATE TABLE \"TalliedRow\" (\"Id\" INTEGER PRIMARY KEY)", []).ExecuteNonQuery();
        db.CreateCommand("INSERT INTO \"TalliedRow\" (\"Id\") VALUES (1)", []).ExecuteNonQuery();

        db.Schema.Migrate<TalliedRow>(m => m.Set(e => e.Score, new PointTally(42)));

        PointTally stored = db.Table<TalliedRow>().First().Score;
        Assert.Equal(42, stored.Value);
    }
}

public class CentsAmountRow
{
    [Key]
    public int Id { get; set; }

    [DefaultValue(typeof(decimal), "9.99")]
    public decimal Amount { get; set; }
}

public class TalliedRow
{
    [Key]
    public int Id { get; set; }

    public PointTally Score { get; set; } = new PointTally(0);
}

public readonly struct PointTally
{
    public PointTally(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

public class CentsConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public object? ToDatabase(object? value)
    {
        return value is decimal d ? (long)(d * 100) : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? l / 100m : 0m;
    }
}

public class PointTallyConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public object? ToDatabase(object? value)
    {
        return value is PointTally t ? (long)t.Value : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new PointTally((int)l) : new PointTally(0);
    }
}
