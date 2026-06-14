using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class EnumMethodRow
{
    [Key]
    public int Id { get; set; }

    public EnumMethodColor Color { get; set; }
}

internal enum EnumMethodColor
{
    Red = 0,
    Green = 1,
    Blue = 2
}

public class EnumInstanceMethodProjectionTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<EnumMethodRow>().Schema.CreateTable();
        db.Table<EnumMethodRow>().Add(new EnumMethodRow { Id = 1, Color = EnumMethodColor.Green });
        db.Table<EnumMethodRow>().Add(new EnumMethodRow { Id = 2, Color = EnumMethodColor.Blue });
        return db;
    }

    [Fact]
    public void GetTypeCodeProjectsUnderlyingTypeCode()
    {
        using TestDatabase db = SetupDatabase();

        List<TypeCode> expected = db.Table<EnumMethodRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => r.Color.GetTypeCode())
            .ToList();

        Assert.Equal([TypeCode.Int32, TypeCode.Int32], expected);

        List<TypeCode> actual = db.Table<EnumMethodRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Color.GetTypeCode())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompareToProjectsComparison()
    {
        using TestDatabase db = SetupDatabase();

        List<int> expected = db.Table<EnumMethodRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => r.Color.CompareTo(EnumMethodColor.Green))
            .ToList();

        List<int> actual = db.Table<EnumMethodRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Color.CompareTo(EnumMethodColor.Green))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EqualsProjectsEquality()
    {
        using TestDatabase db = SetupDatabase();

        List<bool> expected = db.Table<EnumMethodRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => r.Color.Equals(EnumMethodColor.Green))
            .ToList();

        List<bool> actual = db.Table<EnumMethodRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Color.Equals(EnumMethodColor.Green))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
