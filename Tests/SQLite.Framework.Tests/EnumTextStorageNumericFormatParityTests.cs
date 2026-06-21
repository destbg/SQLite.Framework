using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
internal enum EtsPerm
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4
}

internal enum EtsColor
{
    Red = 1,
    Green = 2,
    Blue = 4
}

internal sealed class EtsPermRow
{
    [Key]
    public int Id { get; set; }

    public EtsPerm Perms { get; set; }
}

internal sealed class EtsColorRow
{
    [Key]
    public int Id { get; set; }

    public EtsColor C { get; set; }
}

public class EnumTextStorageNumericFormatParityTests
{
    [Fact]
    public void FlagsEnumToStringDecimal_TextStorage_ReturnsStoredName()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<EtsPermRow>().Schema.CreateTable();
        db.Table<EtsPermRow>().Add(new EtsPermRow { Id = 1, Perms = EtsPerm.Read | EtsPerm.Write });

        string actual = db.Table<EtsPermRow>().Select(r => r.Perms.ToString("D")).First();

        Assert.Equal("Read, Write", actual);
    }

    [Fact]
    public void FlagsEnumToStringHex_TextStorage_ReturnsStoredName()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<EtsPermRow>().Schema.CreateTable();
        db.Table<EtsPermRow>().Add(new EtsPermRow { Id = 1, Perms = EtsPerm.Read | EtsPerm.Write });

        string actual = db.Table<EtsPermRow>().Select(r => r.Perms.ToString("X")).First();

        Assert.Equal("Read, Write", actual);
    }

    [Fact]
    public void UndefinedEnumToStringHex_TextStorage_ReturnsStoredNumber()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<EtsColorRow>().Schema.CreateTable();
        db.Table<EtsColorRow>().Add(new EtsColorRow { Id = 1, C = (EtsColor)99 });

        string actual = db.Table<EtsColorRow>().Select(r => r.C.ToString("X")).First();

        Assert.Equal("99", actual);
    }

    [Fact]
    public void SingleMemberToStringDecimal_TextStorage_MatchesLinqToObjects()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<EtsColorRow>().Schema.CreateTable();
        db.Table<EtsColorRow>().Add(new EtsColorRow { Id = 1, C = EtsColor.Green });

        string oracle = EtsColor.Green.ToString("D");
        string actual = db.Table<EtsColorRow>().Select(r => r.C.ToString("D")).First();

        Assert.Equal(oracle, actual);
    }
}
