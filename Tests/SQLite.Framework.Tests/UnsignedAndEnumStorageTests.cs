using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("UnsignedStorageRows")]
file sealed class UnsignedStorageRow
{
    [Key]
    public int Id { get; set; }
    public uint UIntValue { get; set; }
    public ulong ULongValue { get; set; }
}

public enum LongBackedEnum : long
{
    Small = 0,
    Huge = 1L << 40
}

[Table("LongBackedEnumRows")]
file sealed class LongBackedEnumRow
{
    [Key]
    public int Id { get; set; }
    public LongBackedEnum Value { get; set; }
}

public class UnsignedAndEnumStorageTests
{
    [Fact]
    public void UlongAboveLongMaxRoundTrips()
    {
        using TestDatabase db = new();
        db.Table<UnsignedStorageRow>().Schema.CreateTable();
        db.Table<UnsignedStorageRow>().Add(new UnsignedStorageRow { Id = 1, ULongValue = ulong.MaxValue });

        UnsignedStorageRow row = db.Table<UnsignedStorageRow>().First(x => x.Id == 1);

        Assert.Equal(ulong.MaxValue, row.ULongValue);
    }

    [Fact]
    public void UintAboveIntMaxRoundTrips()
    {
        using TestDatabase db = new();
        db.Table<UnsignedStorageRow>().Schema.CreateTable();
        db.Table<UnsignedStorageRow>().Add(new UnsignedStorageRow { Id = 1, UIntValue = uint.MaxValue });

        UnsignedStorageRow row = db.Table<UnsignedStorageRow>().First(x => x.Id == 1);

        Assert.Equal(uint.MaxValue, row.UIntValue);
    }

    [Fact]
    public void EnumWithLongUnderlyingValueRoundTrips()
    {
        using TestDatabase db = new();
        db.Table<LongBackedEnumRow>().Schema.CreateTable();
        db.Table<LongBackedEnumRow>().Add(new LongBackedEnumRow { Id = 1, Value = LongBackedEnum.Huge });

        LongBackedEnumRow row = db.Table<LongBackedEnumRow>().First(x => x.Id == 1);

        Assert.Equal(LongBackedEnum.Huge, row.Value);
    }
}
