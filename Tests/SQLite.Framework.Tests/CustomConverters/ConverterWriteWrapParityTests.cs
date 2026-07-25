using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal readonly struct WrapOffsetVal
{
    public WrapOffsetVal(int n) => N = n;

    public int N { get; }
}

internal sealed class WrapOffsetConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value) => value is WrapOffsetVal v ? (long)v.N : null;

    public object? FromDatabase(object? value) => value is long l ? new WrapOffsetVal((int)l) : new WrapOffsetVal(0);
}

[Table("WrapOffsetT")]
internal sealed class WrapOffsetBase
{
    [Key]
    public int Id { get; set; }
}

[Table("WrapOffsetT")]
internal sealed class WrapOffsetRow
{
    [Key]
    public int Id { get; set; }

    public WrapOffsetVal Value { get; set; }
}

internal sealed class WrapTriggerSource
{
    [Key]
    public int Id { get; set; }
}

internal sealed class WrapTriggerAudit
{
    [Key]
    public int Id { get; set; }

    public WrapOffsetVal Value { get; set; }
}

public class ConverterWriteWrapParityTests
{
    [Fact]
    public void NormalWriteRoundTripsThroughConverterWrap()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<WrapOffsetVal>(new WrapOffsetConverter()));
        db.Table<WrapOffsetRow>().Schema.CreateTable();
        db.Table<WrapOffsetRow>().Add(new WrapOffsetRow { Id = 1, Value = new WrapOffsetVal(9) });

        WrapOffsetVal read = db.Table<WrapOffsetRow>().Select(x => x.Value).Single();

        Assert.Equal(9, read.N);
    }

    [Fact]
    public void AddColumnDefaultDoesNotApplyConverterWrap()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<WrapOffsetVal>(new WrapOffsetConverter()));
        db.Table<WrapOffsetBase>().Schema.CreateTable();
        db.Table<WrapOffsetBase>().Add(new WrapOffsetBase { Id = 1 });

        db.Schema.AddColumn<WrapOffsetRow>(nameof(WrapOffsetRow.Value), defaultValue: new WrapOffsetVal(5));

        WrapOffsetVal read = db.Table<WrapOffsetRow>().Select(x => x.Value).Single();

        Assert.Equal(-995, read.N);
    }

#if !SQLITECIPHER
    [Fact]
    public void TriggerConstantWriteAppliesConverterWrap()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<WrapOffsetVal>(new WrapOffsetConverter()));
        db.Table<WrapTriggerSource>().Schema.CreateTable();
        db.Table<WrapTriggerAudit>().Schema.CreateTable();

        WrapOffsetVal constant = new(5);
        db.Schema.CreateTrigger<WrapTriggerSource>("trg_wrap", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
            .Insert(db.Table<WrapTriggerAudit>(), s => s
                .Set(a => a.Value, _ => constant)));

        db.Table<WrapTriggerSource>().Add(new WrapTriggerSource { Id = 1 });

        WrapOffsetVal read = db.Table<WrapTriggerAudit>().Select(a => a.Value).First();

        Assert.Equal(5, read.N);
    }
#endif
}
