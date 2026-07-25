using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Attributes;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[RTreeIndex]
[Table("H21jIntGrid")]
public class H21jIntGrid
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public int MinX { get; set; }

    [RTreeMax("X")]
    public int MaxX { get; set; }

    [RTreeMin("Y")]
    public int MinY { get; set; }

    [RTreeMax("Y")]
    public int MaxY { get; set; }
}

public class RTreeIntegerCoordinateValidationTests
{
    [Fact]
    public void FreshlyCreatedTableWithIntegerCoordinatesIsValid()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<H21jIntGrid>();

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H21jIntGrid>();

        Assert.True(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Fact]
    public void IntegerCoordinatesRoundTripThroughTheCreatedTable()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<H21jIntGrid>();

        List<H21jIntGrid> written =
        [
            new H21jIntGrid { Id = 1, MinX = 1, MaxX = 4, MinY = 2, MaxY = 5 },
            new H21jIntGrid { Id = 2, MinX = 7, MaxX = 9, MinY = 3, MaxY = 8 },
        ];
        db.Table<H21jIntGrid>().AddRange(written);

        List<(int Id, int MinX, int MaxY)> expected = written
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, r.MinX, r.MaxY))
            .ToList();
        List<(int Id, int MinX, int MaxY)> actual = db.Table<H21jIntGrid>()
            .OrderBy(r => r.Id)
            .AsEnumerable()
            .Select(r => (r.Id, r.MinX, r.MaxY))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
