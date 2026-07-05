using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RealCoded")]
public class RealCodedRow
{
    [Key]
    public int Id { get; set; }

    public double RealCode { get; set; }
}

[Table("WideCoded")]
public class WideCodedRow
{
    [Key]
    public int Id { get; set; }

    public int Code { get; set; }
}

public class CharCastFromRealTextStorageTests
{
    [Fact]
    public void CastOfADoubleColumnToCharProjectsTheCharacter()
    {
        using TestDatabase db = new();
        db.Table<RealCodedRow>().Schema.CreateTable();
        db.Table<RealCodedRow>().Add(new RealCodedRow { Id = 1, RealCode = 66.0 });

        char actual = db.Table<RealCodedRow>().Select(r => (char)r.RealCode).First();

        Assert.Equal('B', actual);
    }

    [Fact]
    public void CastOfADoubleColumnToCharFiltersTheCharacter()
    {
        using TestDatabase db = new();
        db.Table<RealCodedRow>().Schema.CreateTable();
        db.Table<RealCodedRow>().Add(new RealCodedRow { Id = 1, RealCode = 66.0 });

        Assert.Equal(1, db.Table<RealCodedRow>().Count(r => (char)r.RealCode == 'B'));
    }

    [Fact]
    public void RoundTripCastOfTheNulCharacterReadsZero()
    {
        using TestDatabase db = new();
        db.Table<WideCodedRow>().Schema.CreateTable();
        db.Table<WideCodedRow>().Add(new WideCodedRow { Id = 1, Code = 65536 });

        Assert.Equal(1, db.Table<WideCodedRow>().Count(r => (int)(char)r.Code == 0));
    }
}
