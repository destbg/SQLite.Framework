using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct H23nCopyPoints
{
    public H23nCopyPoints(int n)
    {
        N = n;
    }

    public int N { get; }
}

public sealed class H23nCopyPointsConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value)
    {
        return value is H23nCopyPoints p ? (long)p.N : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new H23nCopyPoints((int)l) : new H23nCopyPoints(0);
    }
}

[Table("H23nCopySourceRows")]
public class H23nCopySourceRow
{
    [Key]
    public int Id { get; set; }

    public H23nCopyPoints Points { get; set; }
}

[Table("H23nCopyTargetRows")]
public class H23nCopyTargetRow
{
    [Key]
    public int Id { get; set; }

    public H23nCopyPoints Points { get; set; }
}

public class ConverterColumnInsertFromQueryWriteTests
{
    [Fact]
    public void InsertFromQueryKeepsTheConverterColumnValue()
    {
        using TestDatabase db = Setup(nameof(InsertFromQueryKeepsTheConverterColumnValue));

        db.Table<H23nCopyTargetRow>().InsertFromQuery(
            db.Table<H23nCopySourceRow>().Select(s => new H23nCopyTargetRow { Id = s.Id, Points = s.Points }));

        List<int> expected = Rows()
            .Select(s => new H23nCopyTargetRow { Id = s.Id, Points = s.Points })
            .OrderBy(r => r.Id)
            .Select(r => r.Points.N)
            .ToList();
        List<int> actual = db.Table<H23nCopyTargetRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Points.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InsertFromQueryAndEntityInsertReadBackTheSameValue()
    {
        using TestDatabase db = Setup(nameof(InsertFromQueryAndEntityInsertReadBackTheSameValue));

        db.Table<H23nCopyTargetRow>().Add(new H23nCopyTargetRow { Id = 100, Points = new H23nCopyPoints(7) });
        db.Table<H23nCopyTargetRow>().InsertFromQuery(
            db.Table<H23nCopySourceRow>()
                .Where(s => s.Id == 1)
                .Select(s => new H23nCopyTargetRow { Id = s.Id, Points = s.Points }));

        List<int> read = db.Table<H23nCopyTargetRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Points.N)
            .ToList();

        Assert.Equal([7, 7], read);
    }

    private static List<H23nCopySourceRow> Rows()
    {
        return
        [
            new H23nCopySourceRow { Id = 1, Points = new H23nCopyPoints(7) },
            new H23nCopySourceRow { Id = 2, Points = new H23nCopyPoints(42) }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddTypeConverter<H23nCopyPoints>(new H23nCopyPointsConverter()), methodName);
        db.Table<H23nCopySourceRow>().Schema.CreateTable();
        db.Table<H23nCopyTargetRow>().Schema.CreateTable();
        db.Table<H23nCopySourceRow>().AddRange(Rows());
        return db;
    }
}
