using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RTreeTests
{
    [Fact]
    public void CreateTable_DefaultFloatStorage_EmitsRtreeUsingClause()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region2D>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Region2D'");
        Assert.Equal("CREATE VIRTUAL TABLE \"Region2D\" USING rtree(\"Id\", \"MinX\", \"MaxX\", \"MinY\", \"MaxY\")", sql);
        Assert.Equal("CREATE VIRTUAL TABLE \"Region2D\" USING rtree(\"Id\", \"MinX\", \"MaxX\", \"MinY\", \"MaxY\")", sql);
        Assert.Equal("CREATE VIRTUAL TABLE \"Region2D\" USING rtree(\"Id\", \"MinX\", \"MaxX\", \"MinY\", \"MaxY\")", sql);
        Assert.Equal("CREATE VIRTUAL TABLE \"Region2D\" USING rtree(\"Id\", \"MinX\", \"MaxX\", \"MinY\", \"MaxY\")", sql);
        Assert.Equal("CREATE VIRTUAL TABLE \"Region2D\" USING rtree(\"Id\", \"MinX\", \"MaxX\", \"MinY\", \"MaxY\")", sql);
        Assert.Equal("CREATE VIRTUAL TABLE \"Region2D\" USING rtree(\"Id\", \"MinX\", \"MaxX\", \"MinY\", \"MaxY\")", sql);
    }

    [Fact]
    public void CreateTable_Int32Storage_EmitsRtreeI32UsingClause()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<RegionIntCell>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'RegionIntCell'");
        Assert.Equal("CREATE VIRTUAL TABLE \"RegionIntCell\" USING rtree_i32(\"Id\", \"MinX\", \"MaxX\", \"MinY\", \"MaxY\")", sql);
    }

    [Fact]
    public void CreateTable_WithAuxiliary_EmitsPlusPrefix()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<RegionWithLabel>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'RegionWithLabel'");
        Assert.Equal("CREATE VIRTUAL TABLE \"RegionWithLabel\" USING rtree(\"Id\", \"MinX\", \"MaxX\", \"MinY\", \"MaxY\", +\"Label\")", sql);
    }

    [Fact]
    public void Add_AndQuery_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region2D>();

        db.Table<Region2D>().Add(new Region2D { Id = 1, MinX = 0, MaxX = 10, MinY = 0, MaxY = 10 });
        db.Table<Region2D>().Add(new Region2D { Id = 2, MinX = 100, MaxX = 110, MinY = 100, MaxY = 110 });

        List<Region2D> hits = db.Table<Region2D>()
            .Where(r => r.MinX <= 5 && r.MaxX >= 5 && r.MinY <= 5 && r.MaxY >= 5)
            .ToList();

        Assert.Single(hits);
        Assert.Equal(1, hits[0].Id);
    }

    [Fact]
    public void Update_ChangesBoundingBox()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region2D>();
        db.Table<Region2D>().Add(new Region2D { Id = 1, MinX = 0, MaxX = 10, MinY = 0, MaxY = 10 });

        Region2D row = db.Table<Region2D>().First();
        row.MaxX = 50;
        row.MaxY = 50;
        db.Table<Region2D>().Update(row);

        Region2D updated = db.Table<Region2D>().First();
        Assert.Equal(50f, updated.MaxX);
        Assert.Equal(50f, updated.MaxY);
    }

    [Fact]
    public void Remove_RemovesRowFromIndex()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region2D>();
        db.Table<Region2D>().Add(new Region2D { Id = 1, MinX = 0, MaxX = 10, MinY = 0, MaxY = 10 });

        db.Table<Region2D>().Remove(db.Table<Region2D>().First());
        Assert.Empty(db.Table<Region2D>().ToList());
    }

    [Fact]
    public void Add_WithAuxiliary_RoundTripsAuxValue()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<RegionWithLabel>();
        db.Table<RegionWithLabel>().Add(new RegionWithLabel
        {
            Id = 1,
            MinX = 0,
            MaxX = 10,
            MinY = 0,
            MaxY = 10,
            Label = "downtown"
        });

        RegionWithLabel row = db.Table<RegionWithLabel>().First();
        Assert.Equal("downtown", row.Label);
    }

    [Fact]
    public void OneDimension_IsValid()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<RegionOneDimension>();

        db.Table<RegionOneDimension>().Add(new RegionOneDimension { Id = 1, Start = 0, End = 10 });
        db.Table<RegionOneDimension>().Add(new RegionOneDimension { Id = 2, Start = 50, End = 60 });

        List<RegionOneDimension> hits = db.Table<RegionOneDimension>()
            .Where(r => r.Start <= 5 && r.End >= 5)
            .ToList();
        Assert.Single(hits);
        Assert.Equal(1, hits[0].Id);
    }

    [Fact]
    public void TableMapping_MissingDimensionPair_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new TableMapping(typeof(RTreeMinWithoutMax), new SQLite.Framework.SQLiteOptionsBuilder(":memory:").Build()));
        Assert.Contains("RTreeMin", ex.Message);
        Assert.Contains("no matching", ex.Message);
    }

    [Fact]
    public void TableMapping_OrphanMax_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new TableMapping(typeof(RTreeMaxWithoutMin), new SQLite.Framework.SQLiteOptionsBuilder(":memory:").Build()));
        Assert.Contains("RTreeMax", ex.Message);
        Assert.Contains("no matching", ex.Message);
    }

    [Fact]
    public void TableMapping_NoKey_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new TableMapping(typeof(RTreeWithoutKey), new SQLite.Framework.SQLiteOptionsBuilder(":memory:").Build()));
        Assert.Contains("[Key]", ex.Message);
    }

    [Fact]
    public void TableMapping_NoDimensions_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new TableMapping(typeof(RTreeWithoutDimensions), new SQLite.Framework.SQLiteOptionsBuilder(":memory:").Build()));
        Assert.Contains("[RTreeMin]", ex.Message);
    }

    [Fact]
    public void TableMapping_TooManyDimensions_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new TableMapping(typeof(RTreeSixDimensions), new SQLite.Framework.SQLiteOptionsBuilder(":memory:").Build()));
        Assert.Contains("at most 5", ex.Message);
    }

    [Fact]
    public void TableMapping_WrongCoordinateType_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new TableMapping(typeof(RTreeStringCoord), new SQLite.Framework.SQLiteOptionsBuilder(":memory:").Build()));
        Assert.Contains("supported R-Tree coordinate type", ex.Message);
    }

    [Fact]
    public void TableMapping_Int32StorageWithDoubleCoord_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new TableMapping(typeof(RTreeInt32WithDouble), new SQLite.Framework.SQLiteOptionsBuilder(":memory:").Build()));
        Assert.Contains("Int32 storage", ex.Message);
    }

    [Fact]
    public void TableMapping_MultipleKeys_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new TableMapping(typeof(RTreeTwoKeys), new SQLite.Framework.SQLiteOptionsBuilder(":memory:").Build()));
        Assert.Contains("more than one [Key]", ex.Message);
    }

    [Fact]
    public void TableMapping_BadKeyType_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new TableMapping(typeof(RTreeStringKey), new SQLite.Framework.SQLiteOptionsBuilder(":memory:").Build()));
        Assert.Contains("not int or long", ex.Message);
    }

    [Fact]
    public void TableMapping_UnmarkedProperty_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new TableMapping(typeof(RTreeUnmarkedProperty), new SQLite.Framework.SQLiteOptionsBuilder(":memory:").Build()));
        Assert.Contains("no R-Tree role", ex.Message);
    }

    [Fact]
    public void TableMapping_DoubleRoleAttribute_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new TableMapping(typeof(RTreeDoubleRole), new SQLite.Framework.SQLiteOptionsBuilder(":memory:").Build()));
        Assert.Contains("more than one role attribute", ex.Message);
    }

    [Fact]
    public void TableMapping_FtsAndRtree_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new TableMapping(typeof(BothFtsAndRtree), new SQLite.Framework.SQLiteOptionsBuilder(":memory:").Build()));
        Assert.Contains("at most one virtual table kind", ex.Message);
    }

    [Fact]
    public void Add_DoubleCoordinates_RoundTrips()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region2DDouble>();
        db.Table<Region2DDouble>().Add(new Region2DDouble { Id = 1, MinX = 0.5, MaxX = 9.5, MinY = 0.25, MaxY = 9.75 });

        Region2DDouble row = db.Table<Region2DDouble>().First();
        Assert.Equal(0.5, row.MinX);
        Assert.Equal(9.75, row.MaxY);
    }

    [Fact]
    public void TableInfo_ExposesDimensionAndIsMinForBounds()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping<Region2D>();

        Assert.NotNull(mapping.RTree);
        Assert.Equal(4, mapping.RTree.Bounds.Count);
        Assert.Equal("X", mapping.RTree.Bounds[0].Dimension);
        Assert.True(mapping.RTree.Bounds[0].IsMin);
        Assert.Equal("X", mapping.RTree.Bounds[1].Dimension);
        Assert.False(mapping.RTree.Bounds[1].IsMin);
        Assert.Equal("Y", mapping.RTree.Bounds[2].Dimension);
        Assert.True(mapping.RTree.Bounds[2].IsMin);
    }

    [Fact]
    public void ColumnAttribute_RenamesRTreeColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<RegionRenamed>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'RegionRenamed'");
        Assert.Equal("CREATE VIRTUAL TABLE \"RegionRenamed\" USING rtree(\"Id\", \"xmin\", \"xmax\")", sql);
        Assert.Equal("CREATE VIRTUAL TABLE \"RegionRenamed\" USING rtree(\"Id\", \"xmin\", \"xmax\")", sql);
    }

    [Fact]
    public void NotMapped_PropertyIsIgnored()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<RegionWithIgnored>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'RegionWithIgnored'");
        Assert.Equal("CREATE VIRTUAL TABLE \"RegionWithIgnored\" USING rtree(\"Id\", \"MinX\", \"MaxX\")", sql);
    }

    [Fact]
    public void NullableLongKey_IsAccepted()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<RegionNullableLongKey>();
        db.Table<RegionNullableLongKey>().Add(new RegionNullableLongKey { Id = 1L, MinX = 0, MaxX = 10 });

        RegionNullableLongKey row = db.Table<RegionNullableLongKey>().First();
        Assert.Equal(1L, row.Id);
    }
}

[RTreeIndex]
public class Region2D
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
    [RTreeMin("Y")] public float MinY { get; set; }
    [RTreeMax("Y")] public float MaxY { get; set; }
}

[RTreeIndex]
public class Region2DDouble
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public double MinX { get; set; }
    [RTreeMax("X")] public double MaxX { get; set; }
    [RTreeMin("Y")] public double MinY { get; set; }
    [RTreeMax("Y")] public double MaxY { get; set; }
}

[RTreeIndex(SQLiteRTreeStorage.Int32)]
public class RegionIntCell
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public int MinX { get; set; }
    [RTreeMax("X")] public int MaxX { get; set; }
    [RTreeMin("Y")] public int MinY { get; set; }
    [RTreeMax("Y")] public int MaxY { get; set; }
}

[RTreeIndex]
public class RegionWithLabel
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
    [RTreeMin("Y")] public float MinY { get; set; }
    [RTreeMax("Y")] public float MaxY { get; set; }
    [RTreeAuxiliary] public string? Label { get; set; }
}

[RTreeIndex]
public class RegionOneDimension
{
    [Key] public int Id { get; set; }
    [RTreeMin("T")] public float Start { get; set; }
    [RTreeMax("T")] public float End { get; set; }
}

[RTreeIndex]
public class RTreeMinWithoutMax
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
    [RTreeMin("Y")] public float MinY { get; set; }
}

[RTreeIndex]
public class RTreeMaxWithoutMin
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
    [RTreeMax("Y")] public float MaxY { get; set; }
}

[RTreeIndex]
public class RTreeWithoutKey
{
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
}

[RTreeIndex]
public class RTreeWithoutDimensions
{
    [Key] public int Id { get; set; }
    [RTreeAuxiliary] public string? Label { get; set; }
}

[RTreeIndex]
public class RTreeSixDimensions
{
    [Key] public int Id { get; set; }
    [RTreeMin("A")] public float MinA { get; set; }
    [RTreeMax("A")] public float MaxA { get; set; }
    [RTreeMin("B")] public float MinB { get; set; }
    [RTreeMax("B")] public float MaxB { get; set; }
    [RTreeMin("C")] public float MinC { get; set; }
    [RTreeMax("C")] public float MaxC { get; set; }
    [RTreeMin("D")] public float MinD { get; set; }
    [RTreeMax("D")] public float MaxD { get; set; }
    [RTreeMin("E")] public float MinE { get; set; }
    [RTreeMax("E")] public float MaxE { get; set; }
    [RTreeMin("F")] public float MinF { get; set; }
    [RTreeMax("F")] public float MaxF { get; set; }
}

[RTreeIndex]
public class RTreeStringCoord
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public required string MinX { get; set; }
    [RTreeMax("X")] public required string MaxX { get; set; }
}

[RTreeIndex(SQLiteRTreeStorage.Int32)]
public class RTreeInt32WithDouble
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public double MinX { get; set; }
    [RTreeMax("X")] public double MaxX { get; set; }
}

[RTreeIndex]
public class RTreeTwoKeys
{
    [Key] public int Id { get; set; }
    [Key] public int AnotherId { get; set; }
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
}

[RTreeIndex]
public class RTreeStringKey
{
    [Key] public required string Id { get; set; }
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
}

[RTreeIndex]
public class RTreeUnmarkedProperty
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
    public float UnmarkedExtra { get; set; }
}

[RTreeIndex]
public class RTreeDoubleRole
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] [RTreeMax("X")] public float Both { get; set; }
    [RTreeMin("Y")] public float MinY { get; set; }
    [RTreeMax("Y")] public float MaxY { get; set; }
}

[RTreeIndex]
public class RegionRenamed
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] [Column("xmin")] public float MinX { get; set; }
    [RTreeMax("X")] [Column("xmax")] public float MaxX { get; set; }
}

[RTreeIndex]
public class RegionWithIgnored
{
    [Key] public int Id { get; set; }
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
    [NotMapped] public string IgnoredField { get; set; } = "";
}

[RTreeIndex]
public class RegionNullableLongKey
{
    [Key] public long? Id { get; set; }
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
}

[RTreeIndex]
[FullTextSearch]
public class BothFtsAndRtree
{
    [Key] public int Id { get; set; }
    [FullTextIndexed] public required string Body { get; set; }
    [RTreeMin("X")] public float MinX { get; set; }
    [RTreeMax("X")] public float MaxX { get; set; }
}
