using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct H23nTriggerPoints
{
    public H23nTriggerPoints(int n)
    {
        N = n;
    }

    public int N { get; }
}

public sealed class H23nTriggerPointsConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value)
    {
        return value is H23nTriggerPoints p ? (long)p.N : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new H23nTriggerPoints((int)l) : new H23nTriggerPoints(0);
    }
}

[Table("H23nTriggerSourceRows")]
public class H23nTriggerSourceRow
{
    [Key]
    public int Id { get; set; }
}

[Table("H23nTriggerAuditRows")]
public class H23nTriggerAuditRow
{
    [Key]
    public int Id { get; set; }

    public H23nTriggerPoints Points { get; set; }
}

public class ConverterComputedTriggerValueWriteTests
{
    [Fact]
    public void TriggerWritingAChosenConverterConstantKeepsTheValue()
    {
        using TestDatabase db = Setup(nameof(TriggerWritingAChosenConverterConstantKeepsTheValue));
        H23nTriggerPoints first = new(11);
        H23nTriggerPoints second = new(22);

        db.Schema.CreateTrigger<H23nTriggerSourceRow>("h23n_trg_choice", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
            .Insert(db.Table<H23nTriggerAuditRow>(), s => s
                .Set(a => a.Id, _ => t.New.Id)
                .Set(a => a.Points, _ => t.New.Id == 1 ? first : second)));

        List<H23nTriggerSourceRow> source = SourceRows();
        db.Table<H23nTriggerSourceRow>().AddRange(source);

        List<int> expected = source
            .OrderBy(r => r.Id)
            .Select(r => (r.Id == 1 ? first : second).N)
            .ToList();
        List<int> actual = db.Table<H23nTriggerAuditRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Points.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TriggerWritingASingleConverterConstantKeepsTheValue()
    {
        using TestDatabase db = Setup(nameof(TriggerWritingASingleConverterConstantKeepsTheValue));
        H23nTriggerPoints only = new(33);

        db.Schema.CreateTrigger<H23nTriggerSourceRow>("h23n_trg_single", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
            .Insert(db.Table<H23nTriggerAuditRow>(), s => s
                .Set(a => a.Id, _ => t.New.Id)
                .Set(a => a.Points, _ => only)));

        List<H23nTriggerSourceRow> source = SourceRows();
        db.Table<H23nTriggerSourceRow>().AddRange(source);

        List<int> expected = source.OrderBy(r => r.Id).Select(_ => only.N).ToList();
        List<int> actual = db.Table<H23nTriggerAuditRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Points.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23nTriggerSourceRow> SourceRows()
    {
        return
        [
            new H23nTriggerSourceRow { Id = 1 },
            new H23nTriggerSourceRow { Id = 2 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddTypeConverter<H23nTriggerPoints>(new H23nTriggerPointsConverter()), methodName);
        db.Table<H23nTriggerSourceRow>().Schema.CreateTable();
        db.Table<H23nTriggerAuditRow>().Schema.CreateTable();
        return db;
    }
}
