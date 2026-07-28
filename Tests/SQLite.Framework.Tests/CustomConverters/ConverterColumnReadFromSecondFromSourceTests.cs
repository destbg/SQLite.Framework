using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct H23pOffsetValue
{
    public H23pOffsetValue(int n)
    {
        N = n;
    }

    public int N { get; }
}

public sealed class H23pOffsetConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value)
    {
        return value is H23pOffsetValue v ? (long)v.N : null;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? new H23pOffsetValue((int)l) : new H23pOffsetValue(0);
    }
}

[Table("H23pOffsetOwners")]
public class H23pOffsetOwner
{
    [Key]
    public int Id { get; set; }

    public int PayloadId { get; set; }
}

[Table("H23pOffsetPayloads")]
public class H23pOffsetPayload
{
    [Key]
    public int Id { get; set; }

    public H23pOffsetValue Value { get; set; }
}

public class ConverterColumnReadFromSecondFromSourceTests
{
    [Fact]
    public void SecondFromSourceReadsItsConverterColumnBack()
    {
        using TestDatabase db = Setup(nameof(SecondFromSourceReadsItsConverterColumnBack));

        List<int> expected = (
                from o in Owners()
                from p in Payloads()
                where p.Id == o.PayloadId
                orderby o.Id
                select p.Value)
            .Select(v => v.N)
            .ToList();

        List<int> actual = (
                from o in db.Table<H23pOffsetOwner>()
                from p in db.Table<H23pOffsetPayload>()
                where p.Id == o.PayloadId
                orderby o.Id
                select p.Value)
            .AsEnumerable()
            .Select(v => v.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SecondFromSourceWholeRowReadsItsConverterColumnBack()
    {
        using TestDatabase db = Setup(nameof(SecondFromSourceWholeRowReadsItsConverterColumnBack));

        List<int> expected = (
                from o in Owners()
                from p in Payloads()
                where p.Id == o.PayloadId
                orderby o.Id
                select p)
            .Select(p => p.Value.N)
            .ToList();

        List<int> actual = (
                from o in db.Table<H23pOffsetOwner>()
                from p in db.Table<H23pOffsetPayload>()
                where p.Id == o.PayloadId
                orderby o.Id
                select p)
            .AsEnumerable()
            .Select(p => p.Value.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinedInnerTableReadsItsConverterColumnBack()
    {
        using TestDatabase db = Setup(nameof(JoinedInnerTableReadsItsConverterColumnBack));

        List<int> expected = Owners()
            .Join(Payloads(), o => o.PayloadId, p => p.Id, (o, p) => p.Value)
            .Select(v => v.N)
            .ToList();

        List<int> actual = db.Table<H23pOffsetOwner>()
            .Join(db.Table<H23pOffsetPayload>(), o => o.PayloadId, p => p.Id, (o, p) => p.Value)
            .AsEnumerable()
            .Select(v => v.N)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23pOffsetOwner> Owners()
    {
        return
        [
            new H23pOffsetOwner { Id = 1, PayloadId = 7 },
            new H23pOffsetOwner { Id = 2, PayloadId = 8 }
        ];
    }

    private static List<H23pOffsetPayload> Payloads()
    {
        return
        [
            new H23pOffsetPayload { Id = 7, Value = new H23pOffsetValue(9) },
            new H23pOffsetPayload { Id = 8, Value = new H23pOffsetValue(41) }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddTypeConverter<H23pOffsetValue>(new H23pOffsetConverter()), methodName);
        db.Table<H23pOffsetOwner>().Schema.CreateTable();
        db.Table<H23pOffsetPayload>().Schema.CreateTable();
        db.Table<H23pOffsetOwner>().AddRange(Owners());
        db.Table<H23pOffsetPayload>().AddRange(Payloads());
        return db;
    }
}
