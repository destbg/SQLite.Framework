using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DoUpdateRatedEntry")]
file class DoUpdateRatedEntry
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    [DefaultValue(10)]
    public int Rating { get; set; }
}

public class UpsertDoUpdateDatabaseDefaultTests
{
    [Fact]
    public void UpsertDoUpdateAllFreshInsertBindsIncomingValue()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DoUpdateRatedEntry>();

        db.Table<DoUpdateRatedEntry>().Upsert(
            new DoUpdateRatedEntry { Id = 1, Name = "first" },
            c => c.OnConflict(x => x.Id).DoUpdateAll());

        int actual = db.Table<DoUpdateRatedEntry>().Single(x => x.Id == 1).Rating;

        Assert.Equal(0, actual);
    }

    [Fact]
    public void UpsertDoUpdateSpecificFreshInsertBindsIncomingValue()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<DoUpdateRatedEntry>();

        db.Table<DoUpdateRatedEntry>().Upsert(
            new DoUpdateRatedEntry { Id = 1, Name = "first" },
            c => c.OnConflict(x => x.Id).DoUpdate(s => s.Set(x => x.Name, x => x.Name)));

        int actual = db.Table<DoUpdateRatedEntry>().Single(x => x.Id == 1).Rating;

        Assert.Equal(0, actual);
    }
}
