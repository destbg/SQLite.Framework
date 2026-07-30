using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
[Table("H25nFtsFluentDocs")]
public class H25nFtsFluentDoc
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Title { get; set; } = "";
}

[RTreeIndex]
[Table("H25nRtreeFluentBoxes")]
public class H25nRtreeFluentBox
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public float MinX { get; set; }

    [RTreeMax("X")]
    public float MaxX { get; set; }
}

[RTreeIndex]
[Table("H25nRtreeFluentTaggedBoxes")]
public class H25nRtreeFluentTaggedBox
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public float MinX { get; set; }

    [RTreeMax("X")]
    public float MaxX { get; set; }

    [RTreeAuxiliary]
    public string Label { get; set; } = "";
}

public class VirtualTableFluentColumnNameTests
{
    [Fact]
    public void FullTextSearchTableIsCreatedWithTheFluentColumnName()
    {
        using ModelTestDatabase db = new(model => model.Entity<H25nFtsFluentDoc>()
            .HasColumnName(d => d.Title, "heading"));

        db.Schema.CreateTable<H25nFtsFluentDoc>();

        string sql = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'H25nFtsFluentDocs'")!;

        Assert.Equal(
            "CREATE VIRTUAL TABLE \"H25nFtsFluentDocs\" USING fts5(\"heading\", tokenize='unicode61 remove_diacritics 2')",
            sql);
    }

    [Fact]
    public void FullTextSearchRowRoundTripsThroughTheFluentColumnName()
    {
        using ModelTestDatabase db = new(model => model.Entity<H25nFtsFluentDoc>()
            .HasColumnName(d => d.Title, "heading"));

        db.Schema.CreateTable<H25nFtsFluentDoc>();
        db.Table<H25nFtsFluentDoc>().Add(new H25nFtsFluentDoc { Id = 1, Title = "native aot" });

        List<string> titles = db.Table<H25nFtsFluentDoc>().OrderBy(d => d.Id).Select(d => d.Title).ToList();

        Assert.Equal(new List<string> { "native aot" }, titles);
    }

    [Fact]
    public void RTreeTableIsCreatedWithTheFluentColumnName()
    {
        using ModelTestDatabase db = new(model => model.Entity<H25nRtreeFluentBox>()
            .HasColumnName(b => b.MinX, "x0"));

        db.Schema.CreateTable<H25nRtreeFluentBox>();

        string sql = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'H25nRtreeFluentBoxes'")!;

        Assert.Equal(
            "CREATE VIRTUAL TABLE \"H25nRtreeFluentBoxes\" USING rtree(\"Id\", \"x0\", \"MaxX\")",
            sql);
    }

    [Fact]
    public void RTreeTableIsCreatedWithTheFluentRowIdColumnName()
    {
        using ModelTestDatabase db = new(model => model.Entity<H25nRtreeFluentBox>()
            .HasColumnName(b => b.Id, "rid"));

        db.Schema.CreateTable<H25nRtreeFluentBox>();

        string sql = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'H25nRtreeFluentBoxes'")!;

        Assert.Equal(
            "CREATE VIRTUAL TABLE \"H25nRtreeFluentBoxes\" USING rtree(\"rid\", \"MinX\", \"MaxX\")",
            sql);
    }

    [Fact]
    public void RTreeAuxiliaryColumnIsCreatedWithTheFluentColumnName()
    {
        using ModelTestDatabase db = new(model => model.Entity<H25nRtreeFluentTaggedBox>()
            .HasColumnName(b => b.Label, "tag"));

        db.Schema.CreateTable<H25nRtreeFluentTaggedBox>();

        string sql = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'H25nRtreeFluentTaggedBoxes'")!;

        Assert.Contains("\"tag\"", sql);
        Assert.DoesNotContain("\"Label\"", sql);
    }

    [Fact]
    public void RTreeRowRoundTripsThroughTheFluentColumnName()
    {
        using ModelTestDatabase db = new(model => model.Entity<H25nRtreeFluentBox>()
            .HasColumnName(b => b.MinX, "x0"));

        db.Schema.CreateTable<H25nRtreeFluentBox>();
        db.Table<H25nRtreeFluentBox>().Add(new H25nRtreeFluentBox { Id = 1, MinX = 1.5f, MaxX = 2.5f });

        List<float> mins = db.Table<H25nRtreeFluentBox>().OrderBy(b => b.Id).Select(b => b.MinX).ToList();

        Assert.Equal(new List<float> { 1.5f }, mins);
    }
}
