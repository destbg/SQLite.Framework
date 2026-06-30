using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal readonly struct ColumnSqlValue
{
    public ColumnSqlValue(int n)
    {
        N = n;
    }

    public int N { get; }

    public static bool operator ==(ColumnSqlValue a, ColumnSqlValue b) => a.N == b.N;

    public static bool operator !=(ColumnSqlValue a, ColumnSqlValue b) => a.N != b.N;

    public override bool Equals(object? obj) => obj is ColumnSqlValue v && v.N == N;

    public override int GetHashCode() => N;
}

internal sealed class ColumnSqlValueConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ColumnSqlExpression => "({0} - 1000)";

    public object? ToDatabase(object? value) => value is ColumnSqlValue v ? (long)(v.N + 1000) : null;

    public object? FromDatabase(object? value) => value is long l ? new ColumnSqlValue((int)l) : new ColumnSqlValue(0);
}

internal sealed class ColumnSqlEntity
{
    [Key]
    public int Id { get; set; }

    public ColumnSqlValue Value { get; set; }
}

public class ConverterColumnSqlExpressionEqualityTests
{
    private static readonly ColumnSqlEntity[] Data =
    [
        new ColumnSqlEntity { Id = 1, Value = new ColumnSqlValue(5) },
        new ColumnSqlEntity { Id = 2, Value = new ColumnSqlValue(7) },
    ];

    [Fact]
    public void WhereEqualityWithDivergingColumnSqlExpressionDoesNotMatch()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<ColumnSqlValue>(new ColumnSqlValueConverter()));
        db.Table<ColumnSqlEntity>().Schema.CreateTable();
        foreach (ColumnSqlEntity e in Data)
        {
            db.Table<ColumnSqlEntity>().Add(e);
        }

        ColumnSqlValue target = new(5);

        List<int> actual = db.Table<ColumnSqlEntity>().Where(e => e.Value == target).Select(e => e.Id).ToList();

        Assert.Empty(actual);

        List<ColumnSqlValue> roundTripped = db.Table<ColumnSqlEntity>().OrderBy(e => e.Id).Select(e => e.Value).ToList();
        Assert.Equal([5, 7], roundTripped.Select(v => v.N).ToList());
    }
}
