using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public sealed class HexCodeRow
{
    [Key]
    public int Id { get; set; }

    public string HexCode { get; set; } = "";
}

public class ParseWithNumberStylesTests
{
    [Fact]
    public void IntParse_WithHexNumberStyle_InWhereClause_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<HexCodeRow>().Schema.CreateTable();
        db.Table<HexCodeRow>().Add(new HexCodeRow { Id = 1, HexCode = "FF" });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<HexCodeRow>()
                .Where(x => int.Parse(x.HexCode, NumberStyles.HexNumber) == 255)
                .Select(x => x.Id)
                .ToList());
    }

    [Fact]
    public void IntParse_WithHexNumberStyle_InSelectProjection_RunsClientSide()
    {
        using TestDatabase db = new();
        db.Table<HexCodeRow>().Schema.CreateTable();
        db.Table<HexCodeRow>().Add(new HexCodeRow { Id = 1, HexCode = "FF" });

        int oracle = int.Parse("FF", NumberStyles.HexNumber);

        int actual = db.Table<HexCodeRow>()
            .Where(x => x.Id == 1)
            .Select(x => int.Parse(x.HexCode, NumberStyles.HexNumber))
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void IntParse_SingleStringOverload_StillTranslatesToCast()
    {
        using TestDatabase db = new();
        db.Table<HexCodeRow>().Schema.CreateTable();
        db.Table<HexCodeRow>().Add(new HexCodeRow { Id = 1, HexCode = "255" });

        List<int> oracle = new List<string> { "255" }.Select(int.Parse).ToList();

        List<int> actual = db.Table<HexCodeRow>()
            .Where(x => int.Parse(x.HexCode) == 255)
            .Select(x => int.Parse(x.HexCode))
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DoubleParse_WithCulture_InWhereClause_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<HexCodeRow>().Schema.CreateTable();
        db.Table<HexCodeRow>().Add(new HexCodeRow { Id = 1, HexCode = "1.5" });

        CultureInfo invariant = CultureInfo.InvariantCulture;
        Assert.Throws<NotSupportedException>(() =>
            db.Table<HexCodeRow>()
                .Where(x => double.Parse(x.HexCode, invariant) > 1.0)
                .Select(x => x.Id)
                .ToList());
    }
}
