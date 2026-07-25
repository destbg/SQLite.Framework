using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class CompareCultureRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class StringCompareInvariantCultureParityTests
{
    [Fact]
    public void StringCompareInvariantCulture_UsesByteOrder()
    {
        using TestDatabase db = new();
        db.Table<CompareCultureRow>().Schema.CreateTable();
        db.Table<CompareCultureRow>().Add(new CompareCultureRow { Id = 1, Name = "a" });

        List<int> actual = db.Table<CompareCultureRow>()
            .Select(r => Math.Sign(string.Compare(r.Name, "B", StringComparison.InvariantCulture)))
            .ToList();

        Assert.Equal(new List<int> { 1 }, actual);
    }
}
