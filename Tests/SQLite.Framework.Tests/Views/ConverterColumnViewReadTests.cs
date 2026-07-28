using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct H23nViewPoints
{
    public H23nViewPoints(int n)
    {
        N = n;
    }

    public int N { get; }

    public static bool operator ==(H23nViewPoints a, H23nViewPoints b) => a.N == b.N;

    public static bool operator !=(H23nViewPoints a, H23nViewPoints b) => a.N != b.N;

    public override bool Equals(object? obj) => obj is H23nViewPoints other && other.N == N;

    public override int GetHashCode() => N;
}

public sealed class H23nViewPointsConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value)
    {
        return value is H23nViewPoints p ? (long)p.N : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new H23nViewPoints((int)l) : new H23nViewPoints(0);
    }
}

[Table("H23nViewSourceRows")]
public class H23nViewSourceRow
{
    [Key]
    public int Id { get; set; }

    public H23nViewPoints Points { get; set; }
}

[Table("H23nPointsView")]
public class H23nPointsViewRow
{
    public int Id { get; set; }

    public H23nViewPoints Points { get; set; }
}

public class ConverterColumnViewReadTests
{
    [Fact]
    public void ReadingAConverterColumnThroughAViewKeepsTheValue()
    {
        using TestDatabase db = Setup(nameof(ReadingAConverterColumnThroughAViewKeepsTheValue));

        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => r.Points.N).ToList();
        List<int> actual = db.ReadOnlyTable<H23nPointsViewRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Points.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilteringOnAConverterColumnThroughAViewMatchesTheSameRows()
    {
        using TestDatabase db = Setup(nameof(FilteringOnAConverterColumnThroughAViewMatchesTheSameRows));
        H23nViewPoints target = new(7);

        List<int> expected = Rows().Where(r => r.Points == target).OrderBy(r => r.Id).Select(r => r.Id).ToList();
        List<int> actual = db.ReadOnlyTable<H23nPointsViewRow>()
            .Where(r => r.Points == target)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23nViewSourceRow> Rows()
    {
        return
        [
            new H23nViewSourceRow { Id = 1, Points = new H23nViewPoints(7) },
            new H23nViewSourceRow { Id = 2, Points = new H23nViewPoints(42) }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddTypeConverter<H23nViewPoints>(new H23nViewPointsConverter()), methodName);
        db.Table<H23nViewSourceRow>().Schema.CreateTable();
        db.Table<H23nViewSourceRow>().AddRange(Rows());
        db.Schema.CreateView<H23nPointsViewRow>(() =>
            from s in db.Table<H23nViewSourceRow>()
            select new H23nPointsViewRow { Id = s.Id, Points = s.Points });
        return db;
    }
}
