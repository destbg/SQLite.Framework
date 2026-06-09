using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using Xunit;

namespace SQLite.Framework.Tests;

[Table("DuaRows")]
internal sealed class DuaRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    [DefaultValue(10)]
    public int Rating { get; set; }
}

public class UpsertDoUpdateAllDefaultFilteredColumnTests
{
    [Fact]
    public void DoUpdateAll_DefaultFilteredColumn_UsesIncomingValueNotColumnDefault()
    {
        DuaRow existing = new() { Id = 1, Name = "old", Rating = 7 };
        DuaRow incoming = new() { Id = 1, Name = "new", Rating = 0 };

        var seed = new[] { (existing.Id, existing.Name, existing.Rating) };
        var merged = seed
            .Select(r => r.Id == incoming.Id ? (incoming.Id, incoming.Name, incoming.Rating) : r)
            .Single(r => r.Id == 1);
        int oracleRating = merged.Rating;
        Assert.Equal(0, oracleRating);
        Assert.Equal("new", merged.Name);

        using TestDatabase db = new();
        db.Table<DuaRow>().Schema.CreateTable();
        db.Table<DuaRow>().Add(new DuaRow { Id = existing.Id, Name = existing.Name, Rating = existing.Rating });

        db.Table<DuaRow>().Upsert(
            new DuaRow { Id = incoming.Id, Name = incoming.Name, Rating = incoming.Rating },
            c => c.OnConflict(x => x.Id).DoUpdateAll());

        int actualRating = db.Table<DuaRow>().Where(x => x.Id == 1).Select(x => x.Rating).Single();
        string actualName = db.Table<DuaRow>().Where(x => x.Id == 1).Select(x => x.Name).Single();

        Assert.Equal("new", actualName);
        Assert.Equal(oracleRating, actualRating);
    }
}