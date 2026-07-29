using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct H24cCombinedPoints
{
    public H24cCombinedPoints(int n)
    {
        N = n;
    }

    public int N { get; }
}

public sealed class H24cCombinedPointsConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value)
    {
        return value is H24cCombinedPoints p ? (long)p.N : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new H24cCombinedPoints((int)l) : new H24cCombinedPoints(0);
    }
}

[Table("H24cCombinedSourceRows")]
public class H24cCombinedSourceRow
{
    [Key]
    public int Id { get; set; }

    public H24cCombinedPoints Points { get; set; }
}

[Table("H24cCombinedTargetRows")]
public class H24cCombinedTargetRow
{
    [Key]
    public int Id { get; set; }

    public H24cCombinedPoints Points { get; set; }
}

[Table("H24cCombinedPointsView")]
public class H24cCombinedViewRow
{
    public int Id { get; set; }

    public H24cCombinedPoints Points { get; set; }
}

public class ConverterColumnCombinedSourceWriteWrapTests
{
    [Fact]
    public void InsertFromQueryOverACombinedSourceKeepsEveryConverterValue()
    {
        using TestDatabase db = Setup(nameof(InsertFromQueryOverACombinedSourceKeepsEveryConverterValue));
        db.Table<H24cCombinedTargetRow>().Schema.CreateTable();

        db.Table<H24cCombinedTargetRow>().InsertFromQuery(
            db.Table<H24cCombinedSourceRow>()
                .Where(s => s.Id == 1)
                .Select(s => new H24cCombinedTargetRow { Id = s.Id, Points = s.Points })
                .Concat(db.Table<H24cCombinedSourceRow>()
                    .Where(s => s.Id == 2)
                    .Select(s => new H24cCombinedTargetRow { Id = s.Id, Points = s.Points })));

        List<int> expected = Rows()
            .Where(s => s.Id == 1)
            .Select(s => new H24cCombinedTargetRow { Id = s.Id, Points = s.Points })
            .Concat(Rows()
                .Where(s => s.Id == 2)
                .Select(s => new H24cCombinedTargetRow { Id = s.Id, Points = s.Points }))
            .OrderBy(r => r.Id)
            .Select(r => r.Points.N)
            .ToList();

        List<int> actual = db.Table<H24cCombinedTargetRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Points.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ViewOverACombinedSourceKeepsEveryConverterValue()
    {
        using TestDatabase db = Setup(nameof(ViewOverACombinedSourceKeepsEveryConverterValue));

        db.Schema.CreateView<H24cCombinedViewRow>(() =>
            db.Table<H24cCombinedSourceRow>()
                .Where(s => s.Id == 1)
                .Select(s => new H24cCombinedViewRow { Id = s.Id, Points = s.Points })
                .Concat(db.Table<H24cCombinedSourceRow>()
                    .Where(s => s.Id == 2)
                    .Select(s => new H24cCombinedViewRow { Id = s.Id, Points = s.Points })));

        List<int> expected = Rows()
            .Where(s => s.Id == 1)
            .Select(s => new H24cCombinedViewRow { Id = s.Id, Points = s.Points })
            .Concat(Rows()
                .Where(s => s.Id == 2)
                .Select(s => new H24cCombinedViewRow { Id = s.Id, Points = s.Points }))
            .OrderBy(r => r.Id)
            .Select(r => r.Points.N)
            .ToList();

        List<int> actual = db.ReadOnlyTable<H24cCombinedViewRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Points.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24cCombinedSourceRow> Rows()
    {
        return
        [
            new H24cCombinedSourceRow { Id = 1, Points = new H24cCombinedPoints(7) },
            new H24cCombinedSourceRow { Id = 2, Points = new H24cCombinedPoints(42) }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddTypeConverter<H24cCombinedPoints>(new H24cCombinedPointsConverter()), methodName);
        db.Table<H24cCombinedSourceRow>().Schema.CreateTable();
        db.Table<H24cCombinedSourceRow>().AddRange(Rows());
        return db;
    }
}
