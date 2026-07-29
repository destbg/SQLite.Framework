using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct H24cColumnSetPoints
{
    public H24cColumnSetPoints(int n)
    {
        N = n;
    }

    public int N { get; }
}

public sealed class H24cColumnSetPointsConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value)
    {
        return value is H24cColumnSetPoints p ? (long)p.N : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new H24cColumnSetPoints((int)l) : new H24cColumnSetPoints(0);
    }
}

[Table("H24cColumnSetRows")]
public class H24cColumnSetRow
{
    [Key]
    public int Id { get; set; }

    public H24cColumnSetPoints Points { get; set; }
}

public class ConverterColumnWithColumnsExpressionWriteTests
{
    [Fact]
    public void WithColumnsUpdateWritingAChosenConverterValueKeepsTheValue()
    {
        using TestDatabase db = Setup(nameof(WithColumnsUpdateWritingAChosenConverterValueKeepsTheValue));
        H24cColumnSetPoints first = new(11);
        H24cColumnSetPoints second = new(22);

        foreach (H24cColumnSetRow row in Rows())
        {
            db.Table<H24cColumnSetRow>()
                .WithColumns(c => c.Set(r => r.Points, r => r.Id == 1 ? first : second))
                .Update(row);
        }

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id == 1 ? first : second).N)
            .ToList();

        List<int> actual = db.Table<H24cColumnSetRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Points.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24cColumnSetRow> Rows()
    {
        return
        [
            new H24cColumnSetRow { Id = 1, Points = new H24cColumnSetPoints(3) },
            new H24cColumnSetRow { Id = 2, Points = new H24cColumnSetPoints(4) }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddTypeConverter<H24cColumnSetPoints>(new H24cColumnSetPointsConverter()), methodName);
        db.Table<H24cColumnSetRow>().Schema.CreateTable();
        db.Table<H24cColumnSetRow>().AddRange(Rows());
        return db;
    }
}
