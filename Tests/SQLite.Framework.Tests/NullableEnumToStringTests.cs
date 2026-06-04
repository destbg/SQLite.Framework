using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class NullableEnumToStringTests
{
    [Fact]
    public void NullableEnumToString_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<enumnullTsRow>().Schema.CreateTable();
        List<enumnullTsRow> seed = new()
        {
            new enumnullTsRow { Id = 1, Color = enumnullTsColor.Green },
            new enumnullTsRow { Id = 2, Color = null },
        };
        db.Table<enumnullTsRow>().AddRange(seed);
        List<string?> expected = seed.OrderBy(r => r.Id).Select(r => r.Color.ToString()).ToList();
        List<string?> actual = db.Table<enumnullTsRow>().OrderBy(r => r.Id).Select(r => r.Color.ToString()).ToList();
        Assert.Equal(new List<string?> { "Green", "" }, expected);
        Assert.Equal(expected, actual);
    }
}

public enum enumnullTsColor { Red = 1, Green = 2, Blue = 4 }

public class enumnullTsRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public enumnullTsColor? Color { get; set; } }
