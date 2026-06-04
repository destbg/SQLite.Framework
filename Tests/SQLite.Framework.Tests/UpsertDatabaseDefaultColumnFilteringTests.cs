using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RatedEntry")]
file class RatedEntry
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    [DefaultValue(10)]
    public int Rating { get; set; }
}

public class UpsertDatabaseDefaultColumnFilteringTests
{
    [Fact]
    public void UpsertWithDatabaseDefaultMatchesAddForSameData()
    {
        using TestDatabase reference = new();
        reference.Schema.CreateTable<RatedEntry>();
        reference.Table<RatedEntry>().Add(new RatedEntry { Id = 1, Name = "first" });
        int referenceRating = reference.Table<RatedEntry>().Single(x => x.Id == 1).Rating;

        using TestDatabase db = new();
        db.Schema.CreateTable<RatedEntry>();
        db.Table<RatedEntry>().Upsert(
            new RatedEntry { Id = 1, Name = "first" },
            c => c.OnConflict(x => x.Id).DoNothing());
        int actualRating = db.Table<RatedEntry>().Single(x => x.Id == 1).Rating;

        Assert.Equal(referenceRating, actualRating);
    }

    [Fact]
    public void UpsertRangeWithDatabaseDefaultMatchesAddRangeForSameData()
    {
        using TestDatabase reference = new();
        reference.Schema.CreateTable<RatedEntry>();
        reference.Table<RatedEntry>().AddRange(
        [
            new RatedEntry { Id = 1, Name = "a" },
            new RatedEntry { Id = 2, Name = "b", Rating = 5 },
            new RatedEntry { Id = 3, Name = "c" },
        ]);
        List<int> referenceRatings = reference.Table<RatedEntry>()
            .OrderBy(x => x.Id)
            .Select(x => x.Rating)
            .ToList();

        using TestDatabase db = new();
        db.Schema.CreateTable<RatedEntry>();
        db.Table<RatedEntry>().UpsertRange(
        [
            new RatedEntry { Id = 1, Name = "a" },
            new RatedEntry { Id = 2, Name = "b", Rating = 5 },
            new RatedEntry { Id = 3, Name = "c" },
        ], c => c.OnConflict(x => x.Id).DoNothing());
        List<int> actualRatings = db.Table<RatedEntry>()
            .OrderBy(x => x.Id)
            .Select(x => x.Rating)
            .ToList();

        Assert.Equal(referenceRatings, actualRatings);
    }
}
