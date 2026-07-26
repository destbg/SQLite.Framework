using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22uFillRows")]
public class H22uFillRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Tag { get; set; }
}

[Table("H22uIndexedFillRows")]
public class H22uIndexedFillRow
{
    [Key]
    public int Id { get; set; }

    public string Slug { get; set; } = "";

    public string Note { get; set; } = "";
}

[Table("H22uAttrUniqueRows")]
public class H22uAttrUniqueRow
{
    [Key]
    public int Id { get; set; }

    [SQLite.Framework.Attributes.Indexed(IsUnique = true)]
    public string Code { get; set; } = "";

    [SQLite.Framework.Attributes.Indexed]
    public string Label { get; set; } = "";
}

[Table("H22uComputedDeferredRows")]
public class H22uComputedDeferredRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Extra { get; set; }

    public int Doubled { get; set; }
}

[Table("H22uDeferredRows")]
public class H22uDeferredRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Extra { get; set; }
}

public class MigrationUniqueFillRebuildTests
{
    [Fact]
    public void NotNullTighteningFillRunsInRebuildWhenLiveNullsExist()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"H22uFillRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Tag\" TEXT)");
        db.Execute("INSERT INTO \"H22uFillRows\" (\"Id\", \"Name\", \"Tag\") VALUES (1, NULL, 'a'), (2, 'kept', 'b')");

        db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H22uFillRow>(s => s.Set(r => r.Name, r => (r.Name == null ? "seed" : r.Name) + "-filled")))
            .Migrate();

        List<string> names = db.Table<H22uFillRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList();
        Assert.Equal(["seed-filled", "kept-filled"], names);
    }

    [Fact]
    public void NotNullTighteningFillStaysDemotedWithoutLiveNulls()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"H22uFillRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Tag\" TEXT)");
        db.Execute("INSERT INTO \"H22uFillRows\" (\"Id\", \"Name\", \"Tag\") VALUES (1, 'a', 'x'), (2, 'b', 'y')");

        db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H22uFillRow>(s => s.Set(r => r.Name, r => r.Name + "!")))
            .Migrate();

        List<string> names = db.Table<H22uFillRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList();
        Assert.Equal(["a!", "b!"], names);
    }

    [Fact]
    public void FillOnTableLevelUniqueIndexColumnRunsInRebuild()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<H22uIndexedFillRow>().Index(r => r.Slug, unique: true);
            model.Entity<H22uIndexedFillRow>().Index(r => r.Note);
        });
        db.Execute("CREATE TABLE \"H22uIndexedFillRows\" (\"Id\" INTEGER PRIMARY KEY, \"Slug\" TEXT, \"Note\" TEXT)");
        db.Execute("INSERT INTO \"H22uIndexedFillRows\" (\"Id\", \"Slug\", \"Note\") VALUES (1, 'a', 'n1'), (2, 'b', 'n2')");

        db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H22uIndexedFillRow>(s => s.Set(r => r.Slug, r => r.Slug + "!")))
            .Migrate();

        List<string> slugs = db.Table<H22uIndexedFillRow>().OrderBy(r => r.Id).Select(r => r.Slug).ToList();
        Assert.Equal(["a!", "b!"], slugs);
    }

    [Fact]
    public void FillOnPlainIndexedColumnStaysDemoted()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<H22uIndexedFillRow>().Index(r => r.Slug, unique: true);
            model.Entity<H22uIndexedFillRow>().Index(r => r.Note);
        });
        db.Execute("CREATE TABLE \"H22uIndexedFillRows\" (\"Id\" INTEGER PRIMARY KEY, \"Slug\" TEXT, \"Note\" TEXT)");
        db.Execute("INSERT INTO \"H22uIndexedFillRows\" (\"Id\", \"Slug\", \"Note\") VALUES (1, 'a', 'n1'), (2, 'b', 'n2')");

        db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H22uIndexedFillRow>(s => s.Set(r => r.Note, r => r.Note + "!")))
            .Migrate();

        List<string> notes = db.Table<H22uIndexedFillRow>().OrderBy(r => r.Id).Select(r => r.Note).ToList();
        Assert.Equal(["n1!", "n2!"], notes);
    }

    [Fact]
    public void FillOnColumnLevelUniqueIndexRunsInRebuild()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"H22uAttrUniqueRows\" (\"Id\" INTEGER PRIMARY KEY, \"Code\" TEXT, \"Label\" TEXT)");
        db.Execute("INSERT INTO \"H22uAttrUniqueRows\" (\"Id\", \"Code\", \"Label\") VALUES (1, 'a', 'x'), (2, 'b', 'y')");

        db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H22uAttrUniqueRow>(s => s
                .Set(r => r.Code, r => r.Code + "!")
                .Set(r => r.Label, r => r.Label + "!")))
            .Migrate();

        List<H22uAttrUniqueRow> rows = db.Table<H22uAttrUniqueRow>().OrderBy(r => r.Id).ToList();
        Assert.Equal(["a!", "b!"], rows.Select(r => r.Code).ToList());
        Assert.Equal(["x!", "y!"], rows.Select(r => r.Label).ToList());
    }

    [Fact]
    public void ComputedColumnsAreSkippedWhenAddingMissingColumns()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<H22uComputedDeferredRow>().Computed(r => r.Doubled, r => r.Id * 2));
        db.Execute("CREATE TABLE \"H22uComputedDeferredRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)");
        db.Execute("INSERT INTO \"H22uComputedDeferredRows\" (\"Id\", \"Name\") VALUES (1, NULL), (2, 'b')");

        db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H22uComputedDeferredRow>(s => s.Set(r => r.Extra, "seed")))
            .Version(3, m => m.TableChanged<H22uComputedDeferredRow>(s => s.Set(r => r.Name, r => (r.Name == null ? "n" : r.Name) + "!")))
            .Migrate();

        List<H22uComputedDeferredRow> rows = db.Table<H22uComputedDeferredRow>().OrderBy(r => r.Id).ToList();
        Assert.Equal(["seed", "seed"], rows.Select(r => r.Extra).ToList());
        Assert.Equal([2, 4], rows.Select(r => r.Doubled).ToList());
    }

    [Fact]
    public void MissingColumnIsAddedBeforeTheDeferredRebuild()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"H22uDeferredRows\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)");
        db.Execute("INSERT INTO \"H22uDeferredRows\" (\"Id\", \"Name\") VALUES (1, NULL), (2, 'b')");

        db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H22uDeferredRow>(s => s.Set(r => r.Extra, "seed")))
            .Version(3, m => m.TableChanged<H22uDeferredRow>(s => s.Set(r => r.Name, r => (r.Name == null ? "n" : r.Name) + "!")))
            .Migrate();

        List<H22uDeferredRow> rows = db.Table<H22uDeferredRow>().OrderBy(r => r.Id).ToList();
        Assert.Equal(["seed", "seed"], rows.Select(r => r.Extra).ToList());
        Assert.Equal(["n!", "b!"], rows.Select(r => r.Name).ToList());
    }
}
