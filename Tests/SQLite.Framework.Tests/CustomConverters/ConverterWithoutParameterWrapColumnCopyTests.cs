using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct H26pScore
{
    public H26pScore(int n)
    {
        N = n;
    }

    public int N { get; }
}

public sealed class H26pScoreConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value)
    {
        return value is H26pScore s ? (long)(s.N + 1000) : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new H26pScore((int)l) : new H26pScore(0);
    }
}

[Table("H26pScoreRows")]
public class H26pScoreRow
{
    [Key]
    public int Id { get; set; }

    public H26pScore Score { get; set; }

    public H26pScore Mirror { get; set; }
}

[Table("H26pScoreUpsertRows")]
public class H26pScoreUpsertRow
{
    [Key]
    public int Id { get; set; }

    public H26pScore Score { get; set; }

    public H26pScore Mirror { get; set; }
}

[Table("H26pScoreNullableRows")]
public class H26pScoreNullableRow
{
    [Key]
    public int Id { get; set; }

    public H26pScore? Score { get; set; }

    public H26pScore? Mirror { get; set; }
}

public class ConverterWithoutParameterWrapColumnCopyTests
{
    [Fact]
    public void WithColumnsCopyingAConverterColumnKeepsTheValue()
    {
        using TestDatabase db = new(
            b => b.AddTypeConverter<H26pScore>(new H26pScoreConverter()),
            nameof(WithColumnsCopyingAConverterColumnKeepsTheValue));
        db.Table<H26pScoreRow>().Schema.CreateTable();
        db.Table<H26pScoreRow>().AddRange(Rows());

        foreach (H26pScoreRow row in Rows())
        {
            db.Table<H26pScoreRow>()
                .WithColumns(c => c.Set(r => r.Mirror, r => r.Score))
                .Update(row);
        }

        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => r.Score.N).ToList();
        List<int> actual = db.Table<H26pScoreRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Mirror.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UpsertSetterCopyingAConverterColumnKeepsTheValue()
    {
        using TestDatabase db = new(
            b => b.AddTypeConverter<H26pScore>(new H26pScoreConverter()),
            nameof(UpsertSetterCopyingAConverterColumnKeepsTheValue));
        db.Table<H26pScoreUpsertRow>().Schema.CreateTable();
        db.Table<H26pScoreUpsertRow>().AddRange(UpsertRows());

        foreach (H26pScoreUpsertRow row in UpsertRows())
        {
            db.Table<H26pScoreUpsertRow>().Upsert(row, c => c
                .OnConflict(r => r.Id)
                .DoUpdate(s => s.Set(r => r.Mirror, r => r.Score)));
        }

        List<int> expected = UpsertRows().OrderBy(r => r.Id).Select(r => r.Score.N).ToList();
        List<int> actual = db.Table<H26pScoreUpsertRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Mirror.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WithColumnsCopyingANullableConverterColumnKeepsTheValue()
    {
        using TestDatabase db = new(
            b => b.AddTypeConverter<H26pScore>(new H26pScoreConverter()),
            nameof(WithColumnsCopyingANullableConverterColumnKeepsTheValue));
        db.Table<H26pScoreNullableRow>().Schema.CreateTable();
        db.Table<H26pScoreNullableRow>().AddRange(NullableRows());

        foreach (H26pScoreNullableRow row in NullableRows())
        {
            db.Table<H26pScoreNullableRow>()
                .WithColumns(c => c.Set(r => r.Mirror, r => r.Score))
                .Update(row);
        }

        List<int?> expected = NullableRows().OrderBy(r => r.Id).Select(r => r.Score?.N).ToList();
        List<int?> actual = db.Table<H26pScoreNullableRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Mirror?.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26pScoreRow> Rows()
    {
        return
        [
            new H26pScoreRow { Id = 1, Score = new H26pScore(7) },
            new H26pScoreRow { Id = 2, Score = new H26pScore(42) }
        ];
    }

    private static List<H26pScoreUpsertRow> UpsertRows()
    {
        return
        [
            new H26pScoreUpsertRow { Id = 1, Score = new H26pScore(7) },
            new H26pScoreUpsertRow { Id = 2, Score = new H26pScore(42) }
        ];
    }

    private static List<H26pScoreNullableRow> NullableRows()
    {
        return
        [
            new H26pScoreNullableRow { Id = 1, Score = new H26pScore(7) },
            new H26pScoreNullableRow { Id = 2, Score = null },
            new H26pScoreNullableRow { Id = 3, Score = new H26pScore(0) }
        ];
    }
}
