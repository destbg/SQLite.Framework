using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class EnumToStringFormatTests
{
    [Fact]
    public void EnumToStringDecimalFormat_MatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<enumnullFmtRow>().Schema.CreateTable();
        List<enumnullFmtRow> seed = new()
        {
            new enumnullFmtRow { Id = 1, Color = enumnullFmtColor.Red },
            new enumnullFmtRow { Id = 2, Color = enumnullFmtColor.Green },
            new enumnullFmtRow { Id = 3, Color = enumnullFmtColor.Blue },
        };
        db.Table<enumnullFmtRow>().AddRange(seed);
        List<string> expected = seed.OrderBy(r => r.Id).Select(r => r.Color.ToString("D")).ToList();
        List<string> actual = db.Table<enumnullFmtRow>().OrderBy(r => r.Id).Select(r => r.Color.ToString("D")).ToList();
        Assert.Equal(new List<string> { "1", "2", "4" }, expected);
        Assert.Equal(expected, actual);
    }
}

public enum enumnullFmtColor { Red = 1, Green = 2, Blue = 4 }

public class enumnullFmtRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public enumnullFmtColor Color { get; set; } }
