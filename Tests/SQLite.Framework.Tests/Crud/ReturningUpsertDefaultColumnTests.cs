using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ReturningUpsertDefaultRows")]
file sealed class ReturningUpsertDefaultRow
{
    [Key]
    public int Id { get; set; }

    public required string Title { get; set; }

    [DefaultValue(10)]
    public int Rating { get; set; }
}

public class ReturningUpsertDefaultColumnTests
{
    [Fact]
    public void PlainUpsertAppliesDatabaseDefault()
    {
        using TestDatabase db = new();
        db.Table<ReturningUpsertDefaultRow>().Schema.CreateTable();

        db.Table<ReturningUpsertDefaultRow>().Upsert(
            new ReturningUpsertDefaultRow { Id = 1, Title = "x" },
            c => c.OnConflict(r => r.Id).DoNothing());

        int stored = db.Query<int>("SELECT \"Rating\" FROM \"ReturningUpsertDefaultRows\" WHERE \"Id\" = 1").First();
        Assert.Equal(10, stored);
    }

    [Fact]
    public void ReturningUpsertAppliesDatabaseDefaultLikePlainUpsert()
    {
        using TestDatabase db = new();
        db.Table<ReturningUpsertDefaultRow>().Schema.CreateTable();

        db.Table<ReturningUpsertDefaultRow>().Returning().Upsert(
            new ReturningUpsertDefaultRow { Id = 1, Title = "x" },
            c => c.OnConflict(r => r.Id).DoNothing());

        int stored = db.Query<int>("SELECT \"Rating\" FROM \"ReturningUpsertDefaultRows\" WHERE \"Id\" = 1").First();
        Assert.Equal(10, stored);
    }
}
