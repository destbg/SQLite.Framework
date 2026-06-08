using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GenConvUIntRow")]
public sealed class GenConvUIntRow
{
    [Key]
    public int Id { get; set; }

    public uint Value { get; set; }
}

[Table("GenConvBoolRow")]
public sealed class GenConvBoolRow
{
    [Key]
    public int Id { get; set; }

    public bool Flag { get; set; }
}

public sealed class GenPlusThousandUIntConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public object? ToDatabase(object? value)
    {
        return value;
    }

    public object? FromDatabase(object? value)
    {
        return value is long l ? (uint)(l + 1000) : value;
    }
}

public sealed class GenYesNoBoolConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

    public object? ToDatabase(object? value)
    {
        return value is bool b ? (b ? "yes" : "no") : value;
    }

    public object? FromDatabase(object? value)
    {
        return value is string s ? s == "yes" : value;
    }
}

public class GeneratedConverterPrimitiveParityTests
{
    [Fact]
    public void UIntConverter_GeneratorReachableEntity_AppliedOnRead()
    {
        using TestDatabase db = new(b => b.AddTypeConverter(typeof(uint), new GenPlusThousandUIntConverter()));
        db.Table<GenConvUIntRow>().Schema.CreateTable();
        db.Table<GenConvUIntRow>().Add(new GenConvUIntRow { Id = 1, Value = 5 });

        uint entityRead = db.Table<GenConvUIntRow>().First().Value;
        uint projectionRead = db.Table<GenConvUIntRow>().Select(r => r.Value).First();

        Assert.Equal(1005u, entityRead);
        Assert.Equal(1005u, projectionRead);
    }

    [Fact]
    public void BoolConverter_GeneratorReachableEntity_AppliedOnWrite()
    {
        using TestDatabase db = new(b => b.AddTypeConverter(typeof(bool), new GenYesNoBoolConverter()));
        db.Table<GenConvBoolRow>().Schema.CreateTable();
        db.Table<GenConvBoolRow>().Add(new GenConvBoolRow { Id = 1, Flag = true });
        db.Table<GenConvBoolRow>().Add(new GenConvBoolRow { Id = 2, Flag = false });

        List<string> stored = db.Query<string>("SELECT Flag FROM GenConvBoolRow ORDER BY Id").ToList();

        Assert.Equal(["yes", "no"], stored);
    }

    [Fact]
    public void BoolConverter_GeneratorReachableEntity_AppliedOnAddRange()
    {
        using TestDatabase db = new(b => b.AddTypeConverter(typeof(bool), new GenYesNoBoolConverter()));
        db.Table<GenConvBoolRow>().Schema.CreateTable();
        db.Table<GenConvBoolRow>().AddRange(
        [
            new GenConvBoolRow { Id = 1, Flag = true },
            new GenConvBoolRow { Id = 2, Flag = false },
        ]);

        List<string> stored = db.Query<string>("SELECT Flag FROM GenConvBoolRow ORDER BY Id").ToList();

        Assert.Equal(["yes", "no"], stored);
    }
}
