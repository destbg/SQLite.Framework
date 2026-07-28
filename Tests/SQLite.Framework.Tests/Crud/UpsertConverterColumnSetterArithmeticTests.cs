using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly record struct H23lCounterValue(int N)
{
    public static H23lCounterValue operator +(H23lCounterValue left, H23lCounterValue right)
    {
        return new H23lCounterValue(left.N + right.N);
    }
}

public sealed class H23lCounterConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value)
    {
        return value is H23lCounterValue v ? (long)v.N : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new H23lCounterValue((int)l) : default(H23lCounterValue);
    }
}

[Table("H23lCounterRows")]
public class H23lCounterRow
{
    [Key]
    public int Id { get; set; }

    public H23lCounterValue Total { get; set; }
}

public class UpsertConverterColumnSetterArithmeticTests
{
    [Fact]
    public void DoUpdateSetterAddingBothRowsStoresTheSum()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<H23lCounterValue>(new H23lCounterConverter()));
        db.Table<H23lCounterRow>().Schema.CreateTable();

        H23lCounterRow existing = new() { Id = 1, Total = new H23lCounterValue(3) };
        H23lCounterRow incoming = new() { Id = 1, Total = new H23lCounterValue(7) };
        H23lCounterValue expected = existing.Total + incoming.Total;

        db.Table<H23lCounterRow>().Add(existing);
        db.Table<H23lCounterRow>().Upsert(incoming, c => c
            .OnConflict(r => r.Id)
            .DoUpdate(s => s.Set(r => r.Total, (current, excluded) => current.Total + excluded.Total)));

        H23lCounterValue actual = db.Table<H23lCounterRow>().Single().Total;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DoUpdateSetterCopyingTheIncomingRowStoresTheIncomingValue()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<H23lCounterValue>(new H23lCounterConverter()));
        db.Table<H23lCounterRow>().Schema.CreateTable();

        H23lCounterRow existing = new() { Id = 1, Total = new H23lCounterValue(3) };
        H23lCounterRow incoming = new() { Id = 1, Total = new H23lCounterValue(7) };

        db.Table<H23lCounterRow>().Add(existing);
        db.Table<H23lCounterRow>().Upsert(incoming, c => c
            .OnConflict(r => r.Id)
            .DoUpdate(s => s.Set(r => r.Total, (current, excluded) => excluded.Total)));

        H23lCounterValue actual = db.Table<H23lCounterRow>().Single().Total;

        Assert.Equal(new H23lCounterValue(7), actual);
    }
}
