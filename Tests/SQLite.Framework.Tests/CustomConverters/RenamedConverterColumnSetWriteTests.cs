using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct H23nRenamePoints
{
    public H23nRenamePoints(int n)
    {
        N = n;
    }

    public int N { get; }
}

public sealed class H23nRenamePointsConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value)
    {
        return value is H23nRenamePoints p ? (long)p.N : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new H23nRenamePoints((int)l) : new H23nRenamePoints(0);
    }
}

[Table("H23nRenamedPointRows")]
public class H23nRenamedPointRow
{
    [Key]
    public int Id { get; set; }

    [Column("PointsStored")]
    public H23nRenamePoints Points { get; set; }

    [Column("SourceStored")]
    public H23nRenamePoints Source { get; set; }
}

[Table("H23nPlainPointRows")]
public class H23nPlainPointRow
{
    [Key]
    public int Id { get; set; }

    public H23nRenamePoints Points { get; set; }

    public H23nRenamePoints Source { get; set; }
}

public class RenamedConverterColumnSetWriteTests
{
    [Fact]
    public void CopyingBetweenRenamedConverterColumnsKeepsTheValue()
    {
        using TestDatabase db = Setup(nameof(CopyingBetweenRenamedConverterColumnsKeepsTheValue));

        db.Table<H23nRenamedPointRow>().ExecuteUpdate(s => s.Set(r => r.Points, r => r.Source));

        List<H23nRenamedPointRow> local = RenamedRows();
        foreach (H23nRenamedPointRow row in local)
        {
            row.Points = row.Source;
        }

        List<int> expected = local.OrderBy(r => r.Id).Select(r => r.Points.N).ToList();
        List<int> actual = db.Table<H23nRenamedPointRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Points.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SettingARenamedConverterColumnToAComputedValueKeepsTheValue()
    {
        using TestDatabase db = Setup(nameof(SettingARenamedConverterColumnToAComputedValueKeepsTheValue));
        H23nRenamePoints first = new(11);
        H23nRenamePoints second = new(22);

        db.Table<H23nRenamedPointRow>().ExecuteUpdate(s => s.Set(r => r.Points, r => r.Id == 1 ? first : second));

        List<H23nRenamedPointRow> local = RenamedRows();
        foreach (H23nRenamedPointRow row in local)
        {
            row.Points = row.Id == 1 ? first : second;
        }

        List<int> expected = local.OrderBy(r => r.Id).Select(r => r.Points.N).ToList();
        List<int> actual = db.Table<H23nRenamedPointRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Points.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CopyingBetweenUnrenamedConverterColumnsKeepsTheValue()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<H23nRenamePoints>(new H23nRenamePointsConverter()),
            nameof(CopyingBetweenUnrenamedConverterColumnsKeepsTheValue));
        db.Table<H23nPlainPointRow>().Schema.CreateTable();
        db.Table<H23nPlainPointRow>().AddRange(PlainRows());

        db.Table<H23nPlainPointRow>().ExecuteUpdate(s => s.Set(r => r.Points, r => r.Source));

        List<H23nPlainPointRow> local = PlainRows();
        foreach (H23nPlainPointRow row in local)
        {
            row.Points = row.Source;
        }

        List<int> expected = local.OrderBy(r => r.Id).Select(r => r.Points.N).ToList();
        List<int> actual = db.Table<H23nPlainPointRow>()
            .OrderBy(r => r.Id)
            .ToList()
            .Select(r => r.Points.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23nRenamedPointRow> RenamedRows()
    {
        return
        [
            new H23nRenamedPointRow { Id = 1, Points = new H23nRenamePoints(0), Source = new H23nRenamePoints(5) },
            new H23nRenamedPointRow { Id = 2, Points = new H23nRenamePoints(0), Source = new H23nRenamePoints(9) }
        ];
    }

    private static List<H23nPlainPointRow> PlainRows()
    {
        return
        [
            new H23nPlainPointRow { Id = 1, Points = new H23nRenamePoints(0), Source = new H23nRenamePoints(5) },
            new H23nPlainPointRow { Id = 2, Points = new H23nRenamePoints(0), Source = new H23nRenamePoints(9) }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddTypeConverter<H23nRenamePoints>(new H23nRenamePointsConverter()), methodName);
        db.Table<H23nRenamedPointRow>().Schema.CreateTable();
        db.Table<H23nRenamedPointRow>().AddRange(RenamedRows());
        return db;
    }
}
