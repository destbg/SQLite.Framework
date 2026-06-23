using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal enum ParseUnknownColor
{
    Red = 0,
    Green = 1,
    Blue = 2,
}

internal sealed class ParseUnknownColorRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class EnumParseUnknownNameParityTests
{
    [Fact]
    public void EnumParse_UnknownName_ReadsBackAsZeroValue()
    {
        using TestDatabase db = new();
        db.Table<ParseUnknownColorRow>().Schema.CreateTable();
        db.Table<ParseUnknownColorRow>().Add(new ParseUnknownColorRow { Id = 1, Name = "Purple" });

        List<ParseUnknownColor> actual = db.Table<ParseUnknownColorRow>().Select(r => Enum.Parse<ParseUnknownColor>(r.Name)).ToList();

        Assert.Throws<ArgumentException>(() => Enum.Parse<ParseUnknownColor>("Purple"));
        Assert.Equal(new List<ParseUnknownColor> { ParseUnknownColor.Red }, actual);
    }
}
