using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GeopolyTests
{
    [Fact]
    public void Mapping_KeyBecomesRowid_ShapeBecomesUnderscoreShape()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping<Region>();

        Assert.True(mapping.IsGeopoly);
        Assert.Equal("rowid", mapping.Columns.First(c => c.PropertyInfo.Name == nameof(Region.Id)).Name);
        Assert.Equal("_shape", mapping.Columns.First(c => c.PropertyInfo.Name == nameof(Region.Shape)).Name);
        Assert.Equal("Name", mapping.Columns.First(c => c.PropertyInfo.Name == nameof(Region.Name)).Name);
        Assert.Equal("Population", mapping.Columns.First(c => c.PropertyInfo.Name == nameof(Region.Population)).Name);
    }

    [Fact]
    public void Mapping_RequiresKey()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.TableMapping<RegionWithoutKey>());
        Assert.Contains("[Key]", ex.Message);
    }

    [Fact]
    public void Mapping_RequiresShape()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.TableMapping<RegionWithoutShape>());
        Assert.Contains("[GeopolyShape]", ex.Message);
    }

    [Fact]
    public void Mapping_RejectsKeyAndShapeOnSameProperty()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.TableMapping<RegionKeyAndShapeMixed>());
        Assert.Contains("mutually exclusive", ex.Message);
    }

    [Fact]
    public void Mapping_RejectsMultipleKey()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.TableMapping<RegionTwoKeys>());
        Assert.Contains("more than one [Key]", ex.Message);
    }

    [Fact]
    public void Mapping_RejectsMultipleShape()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.TableMapping<RegionTwoShapes>());
        Assert.Contains("more than one [GeopolyShape]", ex.Message);
    }

    [Fact]
    public void Mapping_RejectsBadKeyType()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.TableMapping<RegionBadKeyType>());
        Assert.Contains("int or long", ex.Message);
    }

    [Fact]
    public void Mapping_RejectsBadShapeType()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.TableMapping<RegionBadShapeType>());
        Assert.Contains("string (GeoJSON) or byte[]", ex.Message);
    }

    [Fact]
    public void Mapping_AcceptsNullableKey()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping<RegionNullableKey>();

        Assert.True(mapping.IsGeopoly);
        Assert.Equal("Id", mapping.Geopoly!.RowIdProperty.Name);
    }

    [Fact]
    public void Mapping_HonorsNotMappedAndColumnAttribute()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping<RegionWithExtras>();

        Assert.True(mapping.IsGeopoly);
        Assert.DoesNotContain(mapping.Geopoly!.Auxiliaries, a => a.Property.Name == nameof(RegionWithExtras.SkipMe));
        GeopolyAuxiliaryColumn renamed = mapping.Geopoly.Auxiliaries.Single(a => a.Property.Name == nameof(RegionWithExtras.LongName));
        Assert.Equal("ln", renamed.ColumnName);
    }

    [Fact]
    public void Mapping_RejectsGeopolyMixedWithOtherVirtualKinds()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.TableMapping<RegionWithBothAttrs>());
        Assert.Contains("at most one virtual table kind", ex.Message);
    }

    [Fact]
    public void GeopolyFunctions_CalledOutsideQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SQLiteGeopolyFunctions.Overlap("a", "b"));
        Assert.Throws<InvalidOperationException>(() => SQLiteGeopolyFunctions.Within("a", "b"));
        Assert.Throws<InvalidOperationException>(() => SQLiteGeopolyFunctions.Area("a"));
        Assert.Throws<InvalidOperationException>(() => SQLiteGeopolyFunctions.ContainsPoint("a", 0, 0));
        Assert.Throws<InvalidOperationException>(() => SQLiteGeopolyFunctions.BoundingBox("a"));
        Assert.Throws<InvalidOperationException>(() => SQLiteGeopolyFunctions.Blob("a"));
        Assert.Throws<InvalidOperationException>(() => SQLiteGeopolyFunctions.Json("a"));
        Assert.Throws<InvalidOperationException>(() => SQLiteGeopolyFunctions.Svg("a", "b"));
        Assert.Throws<InvalidOperationException>(() => SQLiteGeopolyFunctions.CounterClockwise("a"));
        Assert.Throws<InvalidOperationException>(() => SQLiteGeopolyFunctions.Regular(0, 0, 1, 4));
        Assert.Throws<InvalidOperationException>(() => SQLiteGeopolyFunctions.Transform("a", 1, 0, 0, 1, 0, 0));
    }

#if !SQLITE_FRAMEWORK_BUNDLED && !SQLITECIPHER && !NO_SQLITEPCL_RAW_BATTERIES
    [Fact]
    public void LowFloor_BlocksGeopolyFunctions()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_24));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => SQLiteGeopolyFunctions.Area(b.Title))
                .ToList());

        Assert.Contains("SQLiteGeopolyFunctions.Area", ex.Message);
        Assert.Contains("3.27", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksGeopolyVirtualTable()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_22));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Schema.CreateTable<Region>());

        Assert.Contains("Geopoly", ex.Message);
        Assert.Contains("3.27", ex.Message);
    }
#endif

    [Fact]
    public void Mapping_GeopolyTableInfo()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping<Region>();

        Assert.Equal("Regions", mapping.TableName);
        Assert.True(mapping.IsGeopoly);
        Assert.Equal("Id", mapping.Geopoly!.RowIdProperty.Name);
        Assert.Equal("Shape", mapping.Geopoly.ShapeProperty.Name);
        Assert.Equal(2, mapping.Geopoly.Auxiliaries.Count);
        Assert.Equal("Name", mapping.Geopoly.Auxiliaries[0].ColumnName);
        Assert.Equal("Population", mapping.Geopoly.Auxiliaries[1].ColumnName);
    }

#if SQLITE_FRAMEWORK_BUNDLED
    [Fact]
    public void CreateTable_EmitsGeopolyCreate()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region>();

        Assert.True(db.Schema.TableExists<Region>());

        string masterSql = db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE name = 'Regions'")!;
        Assert.Contains("USING geopoly", masterSql);
        Assert.Contains("Name", masterSql);
        Assert.Contains("Population", masterSql);
    }

    [Fact]
    public void Add_And_Query_RoundTrip()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region>();

        db.Table<Region>().Add(new Region
        {
            Shape = "[[0,0],[10,0],[10,10],[0,10],[0,0]]",
            Name = "Box",
            Population = 100,
        });

        List<Region> rows = db.Table<Region>().ToList();
        Assert.Single(rows);
        Assert.Equal("Box", rows[0].Name);
        Assert.Equal(100, rows[0].Population);
        Assert.NotNull(rows[0].Shape);
    }

    [Fact]
    public void GeopolyFunctions_ContainsPoint_FiltersRows()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region>();

        db.Table<Region>().Add(new Region { Shape = "[[0,0],[10,0],[10,10],[0,10],[0,0]]", Name = "Box", Population = 1 });
        db.Table<Region>().Add(new Region { Shape = "[[20,20],[30,20],[30,30],[20,30],[20,20]]", Name = "FarBox", Population = 2 });

        List<Region> inside = db.Table<Region>()
            .Where(r => SQLiteGeopolyFunctions.ContainsPoint(r.Shape, 5, 5))
            .ToList();

        Assert.Single(inside);
        Assert.Equal("Box", inside[0].Name);
    }

    [Fact]
    public void GeopolyFunctions_Area_ComputesArea()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region>();

        db.Table<Region>().Add(new Region { Shape = "[[0,0],[10,0],[10,10],[0,10],[0,0]]", Name = "Box", Population = 1 });

        double area = db.Table<Region>()
            .Select(r => SQLiteGeopolyFunctions.Area(r.Shape))
            .First();

        Assert.Equal(100, area, 5);
    }

    [Fact]
    public void GeopolyFunctions_Overlap_DetectsOverlap()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region>();

        db.Table<Region>().Add(new Region { Shape = "[[0,0],[10,0],[10,10],[0,10],[0,0]]", Name = "A", Population = 1 });
        db.Table<Region>().Add(new Region { Shape = "[[5,5],[15,5],[15,15],[5,15],[5,5]]", Name = "B", Population = 2 });
        db.Table<Region>().Add(new Region { Shape = "[[100,100],[110,100],[110,110],[100,110],[100,100]]", Name = "C", Population = 3 });

        const string probe = "[[2,2],[8,2],[8,8],[2,8],[2,2]]";
        List<string> overlapping = db.Table<Region>()
            .Where(r => SQLiteGeopolyFunctions.Overlap(r.Shape, probe))
            .Select(r => r.Name)
            .ToList();

        Assert.Contains("A", overlapping);
        Assert.Contains("B", overlapping);
        Assert.DoesNotContain("C", overlapping);
    }

    [Fact]
    public void GeopolyFunctions_Json_ProjectsGeoJsonText()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region>();

        db.Table<Region>().Add(new Region { Shape = "[[0,0],[10,0],[10,10],[0,10],[0,0]]", Name = "Box", Population = 1 });

        string json = db.Table<Region>()
            .Select(r => SQLiteGeopolyFunctions.Json(r.Shape))
            .First();

        Assert.StartsWith("[[", json);
    }

    [Fact]
    public void GeopolyFunctions_Regular_BuildsPolygon()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region>();
        db.Table<Region>().Add(new Region { Shape = "[[0,0],[1,0],[1,1],[0,1],[0,0]]", Name = "Tiny", Population = 1 });

        double regularArea = db.Table<Region>()
            .Select(_ => SQLiteGeopolyFunctions.Area(SQLiteGeopolyFunctions.Regular(0, 0, 10, 100)))
            .First();
        Assert.True(regularArea > 300 && regularArea < 320);
    }

    [Fact]
    public void GeopolyFunctions_Within_AndBoundingBox()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region>();
        db.Table<Region>().Add(new Region { Shape = "[[0,0],[10,0],[10,10],[0,10],[0,0]]", Name = "Inner", Population = 1 });

        const string outer = "[[-5,-5],[15,-5],[15,15],[-5,15],[-5,-5]]";
        int within = db.Table<Region>().Select(r => SQLiteGeopolyFunctions.Within(r.Shape, outer)).First();
        Assert.True(within >= 1);

        byte[] bbox = db.Table<Region>().Select(r => SQLiteGeopolyFunctions.BoundingBox(r.Shape)).First();
        Assert.NotNull(bbox);
        Assert.NotEmpty(bbox);
    }

    [Fact]
    public void GeopolyFunctions_BlobAndCcwAndSvgAndTransform()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Region>();
        db.Table<Region>().Add(new Region { Shape = "[[0,0],[10,0],[10,10],[0,10],[0,0]]", Name = "Square", Population = 1 });

        byte[] blob = db.Table<Region>().Select(r => SQLiteGeopolyFunctions.Blob(r.Shape)).First();
        Assert.NotEmpty(blob);

        byte[] ccw = db.Table<Region>().Select(r => SQLiteGeopolyFunctions.CounterClockwise(r.Shape)).First();
        Assert.NotEmpty(ccw);

        string svg = db.Table<Region>().Select(r => SQLiteGeopolyFunctions.Svg(r.Shape, "fill='red'")).First();
        Assert.Contains("points", svg);

        byte[] shifted = db.Table<Region>().Select(r => SQLiteGeopolyFunctions.Transform(r.Shape, 1, 0, 0, 1, 100, 100)).First();
        Assert.NotEmpty(shifted);
    }
#endif
}

[Table("Regions")]
[GeopolyIndex]
public class Region
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [GeopolyShape]
    public required string Shape { get; set; }

    public required string Name { get; set; }
    public int Population { get; set; }
}

[Table("RegionsWithoutKey")]
[GeopolyIndex]
public class RegionWithoutKey
{
    [GeopolyShape]
    public required string Shape { get; set; }
}

[Table("RegionsWithoutShape")]
[GeopolyIndex]
public class RegionWithoutShape
{
    [Key]
    public int Id { get; set; }
}

[Table("RegionsKeyAndShape")]
[GeopolyIndex]
public class RegionKeyAndShapeMixed
{
    [Key]
    [GeopolyShape]
    public required string IdAndShape { get; set; }
}

[Table("RegionsTwoKeys")]
[GeopolyIndex]
public class RegionTwoKeys
{
    [Key]
    public int Id { get; set; }

    [Key]
    public int OtherId { get; set; }

    [GeopolyShape]
    public required string Shape { get; set; }
}

[Table("RegionsTwoShapes")]
[GeopolyIndex]
public class RegionTwoShapes
{
    [Key]
    public int Id { get; set; }

    [GeopolyShape]
    public required string Shape1 { get; set; }

    [GeopolyShape]
    public required string Shape2 { get; set; }
}

[Table("RegionsBadKeyType")]
[GeopolyIndex]
public class RegionBadKeyType
{
    [Key]
    public required string Id { get; set; }

    [GeopolyShape]
    public required string Shape { get; set; }
}

[Table("RegionsBadShapeType")]
[GeopolyIndex]
public class RegionBadShapeType
{
    [Key]
    public int Id { get; set; }

    [GeopolyShape]
    public int Shape { get; set; }
}

[Table("RegionsBothAttrs")]
[GeopolyIndex]
[RTreeIndex]
public class RegionWithBothAttrs
{
    [Key]
    public int Id { get; set; }

    [GeopolyShape]
    public required string Shape { get; set; }
}

[Table("RegionsNullableKey")]
[GeopolyIndex]
public class RegionNullableKey
{
    [Key]
    public int? Id { get; set; }

    [GeopolyShape]
    public required string Shape { get; set; }
}

[Table("RegionsWithExtras")]
[GeopolyIndex]
public class RegionWithExtras
{
    [Key]
    public int Id { get; set; }

    [GeopolyShape]
    public required string Shape { get; set; }

    [Column("ln")]
    public required string LongName { get; set; }

    [NotMapped]
    public string? SkipMe { get; set; }
}
