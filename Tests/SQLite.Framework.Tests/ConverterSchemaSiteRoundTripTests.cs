using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CvSiteIdxUpsRows")]
public class CvSiteIdxUpsRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }

    public string Name { get; set; } = "";

    public int Total { get; set; }
}

[Table("CvSiteCompCopyRows")]
public class CvSiteCompCopyRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }

    public CwPoints PtsCopy { get; set; }
}

[Table("CvSiteDelSrcRows")]
public class CvSiteDelSrcRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }
}

[Table("CvSiteDelRows")]
public class CvSiteDelRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }
}

[Table("CvSiteExclRows")]
public class CvSiteExclRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }

    public string Name { get; set; } = "";
}

[Table("CvSiteExprIdxRows")]
public class CvSiteExprIdxRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }
}

public class ConverterSchemaSiteRoundTripTests
{
    [Fact]
    public void PartialUniqueIndexWithConverterFilterActsAsUpsertConflictTarget()
    {
        CwPoints five = new(5);
        using ModelTestDatabase db = new(
            mb => mb.Entity<CvSiteIdxUpsRow>().Index(r => r.Name, unique: true, filter: r => r.Pts == five),
            b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Table<CvSiteIdxUpsRow>().Schema.CreateTable();
        db.Table<CvSiteIdxUpsRow>().Add(new CvSiteIdxUpsRow { Id = 1, Pts = new CwPoints(5), Name = "x", Total = 1 });

        db.Table<CvSiteIdxUpsRow>().Upsert(
            new CvSiteIdxUpsRow { Id = 2, Pts = new CwPoints(5), Name = "x", Total = 9 },
            c => c.OnConflict(b => b.Name).Where(r => r.Pts == five).DoUpdateAll());

        CvSiteIdxUpsRow stored = db.Table<CvSiteIdxUpsRow>().Single();
        Assert.Equal((1, 5, "x", 9), (stored.Id, stored.Pts.N, stored.Name, stored.Total));
    }

    [Fact]
    public void ComputedColumnOfConverterTypeCopyRoundTrips()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<CvSiteCompCopyRow>().Computed(r => r.PtsCopy, r => r.Pts),
            b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Table<CvSiteCompCopyRow>().Schema.CreateTable();

        db.Table<CvSiteCompCopyRow>().Add(new CvSiteCompCopyRow { Id = 1, Pts = new CwPoints(5) });

        CvSiteCompCopyRow stored = db.Table<CvSiteCompCopyRow>().Single();
        Assert.Equal((5, 5), (stored.Pts.N, stored.PtsCopy.N));
    }

    [Fact]
    public void TriggerDeletePredicateOnConverterColumnMatchesRows()
    {
        CwPoints five = new(5);
        using TestDatabase db = new(b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Table<CvSiteDelSrcRow>().Schema.CreateTable();
        db.Table<CvSiteDelRow>().Schema.CreateTable();

        List<CvSiteDelRow> targets =
        [
            new CvSiteDelRow { Id = 1, Pts = new CwPoints(5) },
            new CvSiteDelRow { Id = 2, Pts = new CwPoints(7) },
        ];
        db.Table<CvSiteDelRow>().AddRange(targets);

        db.Schema.CreateTrigger<CvSiteDelSrcRow>("cvsite_trg_del", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
            .Delete(db.Table<CvSiteDelRow>(), r => r.Pts == five));

        db.Table<CvSiteDelSrcRow>().Add(new CvSiteDelSrcRow { Id = 9, Pts = new CwPoints(1) });

        List<int> expected = targets.Where(r => r.Pts != five).Select(r => r.Id).OrderBy(x => x).ToList();
        List<int> actual = db.Table<CvSiteDelRow>().Select(r => r.Id).OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UpsertDoUpdateWhereExcludedConverterEqualityUpdatesRow()
    {
        CwPoints five = new(5);
        using TestDatabase db = new(b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Table<CvSiteExclRow>().Schema.CreateTable();
        db.Table<CvSiteExclRow>().Add(new CvSiteExclRow { Id = 1, Pts = new CwPoints(7), Name = "before" });

        db.Table<CvSiteExclRow>().Upsert(
            new CvSiteExclRow { Id = 1, Pts = new CwPoints(5), Name = "after" },
            c => c.OnConflict(b => b.Id).DoUpdateAll().Where((r, e) => e.Pts == five));

        Assert.Equal("after", db.Table<CvSiteExclRow>().Select(r => r.Name).Single());
    }

    [Fact]
    public void UpsertDoUpdateWhereExcludedConverterEqualitySkipsRow()
    {
        CwPoints five = new(5);
        using TestDatabase db = new(b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Table<CvSiteExclRow>().Schema.CreateTable();
        db.Table<CvSiteExclRow>().Add(new CvSiteExclRow { Id = 1, Pts = new CwPoints(7), Name = "before" });

        db.Table<CvSiteExclRow>().Upsert(
            new CvSiteExclRow { Id = 1, Pts = new CwPoints(9), Name = "after" },
            c => c.OnConflict(b => b.Id).DoUpdateAll().Where((r, e) => e.Pts == five));

        Assert.Equal("before", db.Table<CvSiteExclRow>().Select(r => r.Name).Single());
    }

    [Fact]
    public void UniqueExpressionIndexOverConverterColumnComparesRestoredValue()
    {
        CwPoints five = new(5);
        using ModelTestDatabase db = new(
            mb => mb.Entity<CvSiteExprIdxRow>().Index(r => r.Pts == five, name: "cvsite_pts_is_five", unique: true),
            b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Table<CvSiteExprIdxRow>().Schema.CreateTable();

        db.Table<CvSiteExprIdxRow>().Add(new CvSiteExprIdxRow { Id = 1, Pts = new CwPoints(5) });
        db.Table<CvSiteExprIdxRow>().Add(new CvSiteExprIdxRow { Id = 2, Pts = new CwPoints(7) });
        Assert.ThrowsAny<Exception>(() => db.Table<CvSiteExprIdxRow>().Add(new CvSiteExprIdxRow { Id = 3, Pts = new CwPoints(8) }));

        Assert.Equal(2, db.Table<CvSiteExprIdxRow>().Count());
    }
}
