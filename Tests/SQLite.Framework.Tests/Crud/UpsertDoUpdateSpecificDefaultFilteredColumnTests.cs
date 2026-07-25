using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using Xunit;

namespace SQLite.Framework.Tests;

[Table("DuRows")]
internal sealed class DuRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    [DefaultValue(10)]
    public int Rating { get; set; }
}

public class UpsertDoUpdateSpecificDefaultFilteredColumnTests
{
    [Fact]
    public void DoUpdateSingleColumn_DefaultFilteredColumn_UsesIncomingValueNotColumnDefault()
    {
        DuRow existing = new() { Id = 1, Name = "old", Rating = 7 };
        DuRow incoming = new() { Id = 1, Name = "new", Rating = 0 };

        var seed = new[] { (existing.Id, existing.Name, existing.Rating) };
        var merged = seed
            .Select(r => r.Id == incoming.Id ? (r.Id, r.Name, incoming.Rating) : r)
            .Single(r => r.Id == 1);
        int oracleRating = merged.Rating;
        string oracleName = merged.Name;
        Assert.Equal(0, oracleRating);
        Assert.Equal("old", oracleName);

        using TestDatabase db = new();
        db.Table<DuRow>().Schema.CreateTable();
        db.Table<DuRow>().Add(new DuRow { Id = existing.Id, Name = existing.Name, Rating = existing.Rating });

        db.Table<DuRow>().Upsert(
            new DuRow { Id = incoming.Id, Name = incoming.Name, Rating = incoming.Rating },
            c => c.OnConflict(x => x.Id).DoUpdate(x => x.Rating));

        int actualRating = db.Table<DuRow>().Where(x => x.Id == 1).Select(x => x.Rating).Single();
        string actualName = db.Table<DuRow>().Where(x => x.Id == 1).Select(x => x.Name).Single();

        Assert.Equal(oracleName, actualName);
        Assert.Equal(oracleRating, actualRating);
    }
}