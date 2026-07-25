using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MultiSourceQueryProjectionTests
{
    [Fact]
    public void ThreeSourceQueryJoinFusedSelect()
    {
        using TestDatabase db = new();
        db.Table<TripletAlpha>().Schema.CreateTable();
        db.Table<TripletBeta>().Schema.CreateTable();
        db.Table<TripletGamma>().Schema.CreateTable();
        db.Table<TripletAlpha>().Add(new TripletAlpha { Id = 1, Name = "a1" });
        db.Table<TripletBeta>().Add(new TripletBeta { Id = 1, AlphaId = 1, Tag = "b1" });
        db.Table<TripletGamma>().Add(new TripletGamma { Id = 1, BetaId = 1, Mark = "c1" });

        var rows = (
            from a in db.Table<TripletAlpha>()
            join b in db.Table<TripletBeta>() on a.Id equals b.AlphaId
            join c in db.Table<TripletGamma>() on b.Id equals c.BetaId
            select new { a.Name, b.Tag, c.Mark }
        ).ToList();

        Assert.Single(rows);
        Assert.Equal(("a1", "b1", "c1"), (rows[0].Name, rows[0].Tag, rows[0].Mark));
    }

    [Fact]
    public void ThreeSourceQueryJoinClientEvalProjection()
    {
        using TestDatabase db = new();
        db.Table<TripletAlpha>().Schema.CreateTable();
        db.Table<TripletBeta>().Schema.CreateTable();
        db.Table<TripletGamma>().Schema.CreateTable();
        db.Table<TripletAlpha>().Add(new TripletAlpha { Id = 1, Name = "a1" });
        db.Table<TripletBeta>().Add(new TripletBeta { Id = 1, AlphaId = 1, Tag = "b1" });
        db.Table<TripletGamma>().Add(new TripletGamma { Id = 1, BetaId = 1, Mark = "c1" });

#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
        long hitsBefore = db.SelectMaterializerHits;
        long fallbacksBefore = db.SelectCompilerFallbacks;
#endif

        var rows = (
            from a in db.Table<TripletAlpha>()
            join b in db.Table<TripletBeta>() on a.Id equals b.AlphaId
            join c in db.Table<TripletGamma>() on b.Id equals c.BetaId
            select new { a.Name, Converted = CommonHelpers.ConvertString(b.Tag), c.Mark }
        ).ToList();

        Assert.Single(rows);
        Assert.Equal(("a1", -1, "c1"), (rows[0].Name, rows[0].Converted, rows[0].Mark));
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
        Assert.True(db.SelectMaterializerHits > hitsBefore,
            "Expected a generated select materializer to handle the fused three source join projection.");
        Assert.Equal(fallbacksBefore, db.SelectCompilerFallbacks);
#endif
    }

    [Fact]
    public void ThreeFromClientEvalProjection()
    {
        using TestDatabase db = new();
        db.Table<TripletAlpha>().Schema.CreateTable();
        db.Table<TripletBeta>().Schema.CreateTable();
        db.Table<TripletGamma>().Schema.CreateTable();
        db.Table<TripletAlpha>().Add(new TripletAlpha { Id = 1, Name = "a1" });
        db.Table<TripletBeta>().Add(new TripletBeta { Id = 1, AlphaId = 1, Tag = "b1" });
        db.Table<TripletGamma>().Add(new TripletGamma { Id = 1, BetaId = 1, Mark = "c1" });

#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
        long hitsBefore = db.SelectMaterializerHits;
        long fallbacksBefore = db.SelectCompilerFallbacks;
#endif

        var rows = (
            from a in db.Table<TripletAlpha>()
            from b in db.Table<TripletBeta>()
            from c in db.Table<TripletGamma>()
            select new { a.Name, Converted = CommonHelpers.ConvertString(b.Tag), c.Mark }
        ).ToList();

        Assert.Single(rows);
        Assert.Equal(("a1", -1, "c1"), (rows[0].Name, rows[0].Converted, rows[0].Mark));
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
        Assert.True(db.SelectMaterializerHits > hitsBefore,
            "Expected a generated select materializer to handle the fused three from projection.");
        Assert.Equal(fallbacksBefore, db.SelectCompilerFallbacks);
#endif
    }

    [Fact]
    public void ThreeFromFusedSelect()
    {
        using TestDatabase db = new();
        db.Table<TripletAlpha>().Schema.CreateTable();
        db.Table<TripletBeta>().Schema.CreateTable();
        db.Table<TripletGamma>().Schema.CreateTable();
        db.Table<TripletAlpha>().Add(new TripletAlpha { Id = 1, Name = "a1" });
        db.Table<TripletBeta>().Add(new TripletBeta { Id = 1, AlphaId = 1, Tag = "b1" });
        db.Table<TripletGamma>().Add(new TripletGamma { Id = 1, BetaId = 1, Mark = "c1" });

        var rows = (
            from a in db.Table<TripletAlpha>()
            from b in db.Table<TripletBeta>()
            from c in db.Table<TripletGamma>()
            select new { a.Name, b.Tag, c.Mark }
        ).ToList();

        Assert.Single(rows);
        Assert.Equal(("a1", "b1", "c1"), (rows[0].Name, rows[0].Tag, rows[0].Mark));
    }

    [Fact]
    public void TwoFromFusedSelect()
    {
        using TestDatabase db = new();
        db.Table<TripletAlpha>().Schema.CreateTable();
        db.Table<TripletBeta>().Schema.CreateTable();
        db.Table<TripletAlpha>().Add(new TripletAlpha { Id = 1, Name = "a1" });
        db.Table<TripletBeta>().Add(new TripletBeta { Id = 1, AlphaId = 1, Tag = "b1" });

        var rows = (
            from a in db.Table<TripletAlpha>()
            from b in db.Table<TripletBeta>()
            select new { a.Name, b.Tag }
        ).ToList();

        Assert.Single(rows);
        Assert.Equal(("a1", "b1"), (rows[0].Name, rows[0].Tag));
    }
}

public class TripletAlpha
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class TripletBeta
{
    public int Id { get; set; }

    public int AlphaId { get; set; }

    public string Tag { get; set; } = string.Empty;
}

public class TripletGamma
{
    public int Id { get; set; }

    public int BetaId { get; set; }

    public string Mark { get; set; } = string.Empty;
}
