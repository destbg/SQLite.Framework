using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CasedColumnNote")]
public class CasedColumnNoteRow
{
    [Key]
    public int Id { get; set; }

    [Column("Body")]
    public string? Body { get; set; }
}

[WithoutRowId]
[Table("KeyedLabel")]
public class KeyedLabelRow
{
    [Key]
    public string Code { get; set; } = "";

    public int Qty { get; set; }
}

[Table("HeldParent")]
public class HeldParentRow
{
    [Key]
    public int Id { get; set; }

    public string? Note { get; set; }
}

[Table("HeldChild")]
public class HeldChildRow
{
    [Key]
    public string Tag { get; set; } = "";

    [ReferencesTable(typeof(HeldParentRow))]
    public int ParentId { get; set; }
}

[Table("HeldIntChild")]
public class HeldIntChildRow
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(HeldParentRow))]
    public int ParentId { get; set; }
}

[WithoutRowId]
[Table("HeldCodeChild")]
public class HeldCodeChildRow
{
    [Key]
    public string Code { get; set; } = "";

    [ReferencesTable(typeof(HeldParentRow))]
    public int ParentId { get; set; }
}

[Table("HeldPairChild")]
public class HeldPairChildRow
{
    [Key]
    public int First { get; set; }

    [Key]
    public string Second { get; set; } = "";

    [ReferencesTable(typeof(HeldParentRow))]
    public int ParentId { get; set; }
}

public class MigrationReconcileSupplementalTests
{
    [Fact]
    public void RenameColumnToTheSameNameIsSkipped()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<CasedColumnNoteRow>().Schema.CreateTable();
        db.Table<CasedColumnNoteRow>().Add(new CasedColumnNoteRow { Id = 1, Body = "kept" });

        db.Schema.Migrations()
            .Version(1, m => m.RenameColumn<CasedColumnNoteRow>("Body", "Body"))
            .Migrate();

        Assert.Equal("kept", db.Table<CasedColumnNoteRow>().Single().Body);
    }

    [Fact]
    public void RenameColumnToTheSameNameInDifferentCaseWorks()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"CasedColumnNote\" (\"Id\" INTEGER PRIMARY KEY, \"BODY\" TEXT)");
        db.Execute("INSERT INTO \"CasedColumnNote\" (\"Id\", \"BODY\") VALUES (1, 'kept')");

        db.Schema.Migrations()
            .Version(1, m => m.RenameColumn<CasedColumnNoteRow>("BODY", "Body"))
            .Migrate();

        List<string> names = db.Pragmas.TableInfo("CasedColumnNote").Select(c => c.Name).ToList();
        Assert.Equal(["Id", "Body"], names);
        Assert.Equal("kept", db.Table<CasedColumnNoteRow>().Single().Body);
    }

    [Fact]
    public void DropColumnWithAnUnquotedTriggerReferenceDropsTheTrigger()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"TrigTrimBook\" (\"Id\" INTEGER PRIMARY KEY, \"Title\" TEXT, \"Legacy\" TEXT)");
        db.Execute("CREATE TRIGGER \"trg_plain_note\" AFTER UPDATE ON \"TrigTrimBook\" BEGIN SELECT OLD.Legacy; END");
        db.Execute("INSERT INTO \"TrigTrimBook\" (\"Id\", \"Title\", \"Legacy\") VALUES (1, 't', 'x')");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<TrigTrimBookRow>("Legacy"))
            .Migrate();

        Exception? ex = Record.Exception(() => db.Execute("UPDATE \"TrigTrimBook\" SET \"Title\" = 'y'"));
        Assert.Null(ex);
    }

    [Fact]
    public void RebuildOfAWithoutRowIdTableKeepsRows()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"KeyedLabel\" (\"Code\" TEXT NOT NULL PRIMARY KEY, \"Qty\" INTEGER NOT NULL, \"Legacy\" TEXT, CHECK (\"Legacy\" IS NULL OR \"Legacy\" <> '')) WITHOUT ROWID");
        db.Execute("INSERT INTO \"KeyedLabel\" (\"Code\", \"Qty\") VALUES ('a', 1), ('b', 2)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<KeyedLabelRow>(rebuild: true))
            .Migrate();

        Assert.Equal(2, db.Table<KeyedLabelRow>().Count());
    }

    [Fact]
    public void RebuildKeepsRowIdsOfAReferencingTableWithoutAnIntegerKey()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"HeldParent\" (\"Id\" INTEGER PRIMARY KEY, \"Note\" TEXT, \"Legacy\" TEXT, CHECK (\"Legacy\" IS NULL OR \"Legacy\" <> ''))");
        db.Table<HeldChildRow>().Schema.CreateTable();
        db.Table<HeldParentRow>().Add(new HeldParentRow { Id = 1 });
        db.Table<HeldChildRow>().AddRange(
        [
            new HeldChildRow { Tag = "a", ParentId = 1 },
            new HeldChildRow { Tag = "b", ParentId = 1 },
        ]);
        db.Execute("DELETE FROM \"HeldChild\" WHERE \"Tag\" = 'a'");
        long before = db.ExecuteScalar<long>("SELECT rowid FROM \"HeldChild\" WHERE \"Tag\" = 'b'");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<HeldParentRow>(s => s.Set(x => x.Note, "filled"), rebuild: true))
            .Migrate();

        long after = db.ExecuteScalar<long>("SELECT rowid FROM \"HeldChild\" WHERE \"Tag\" = 'b'");
        Assert.Equal(before, after);
        Assert.Equal("filled", db.Table<HeldParentRow>().Single().Note);
    }

    [Fact]
    public void RebuildHoldsChildrenOfEveryKeyShape()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"HeldParent\" (\"Id\" INTEGER PRIMARY KEY, \"Note\" TEXT, \"Legacy\" TEXT, CHECK (\"Legacy\" IS NULL OR \"Legacy\" <> ''))");
        db.Table<HeldIntChildRow>().Schema.CreateTable();
        db.Table<HeldCodeChildRow>().Schema.CreateTable();
        db.Table<HeldPairChildRow>().Schema.CreateTable();
        db.Table<HeldParentRow>().Add(new HeldParentRow { Id = 1 });
        db.Table<HeldIntChildRow>().Add(new HeldIntChildRow { Id = 7, ParentId = 1 });
        db.Table<HeldCodeChildRow>().Add(new HeldCodeChildRow { Code = "c", ParentId = 1 });
        db.Table<HeldPairChildRow>().Add(new HeldPairChildRow { First = 1, Second = "s", ParentId = 1 });

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<HeldParentRow>(s => s.Set(x => x.Note, "held"), rebuild: true))
            .Migrate();

        Assert.Equal(7, db.Table<HeldIntChildRow>().Single().Id);
        Assert.Equal("c", db.Table<HeldCodeChildRow>().Single().Code);
        Assert.Equal("s", db.Table<HeldPairChildRow>().Single().Second);
        Assert.Equal("held", db.Table<HeldParentRow>().Single().Note);
    }

    [Fact]
    public void RebuildOfTextAndCompositeKeyTablesKeepsRows()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"HeldParent\" (\"Id\" INTEGER PRIMARY KEY, \"Note\" TEXT)");
        db.Execute("CREATE TABLE \"HeldChild\" (\"Tag\" TEXT NOT NULL PRIMARY KEY, \"ParentId\" INTEGER NOT NULL REFERENCES \"HeldParent\" (\"Id\"), \"Legacy\" TEXT, CHECK (\"Legacy\" IS NULL OR \"Legacy\" <> ''))");
        db.Execute("CREATE TABLE \"HeldPairChild\" (\"First\" INTEGER NOT NULL, \"Second\" TEXT NOT NULL, \"ParentId\" INTEGER NOT NULL REFERENCES \"HeldParent\" (\"Id\"), \"Legacy\" TEXT, CHECK (\"Legacy\" IS NULL OR \"Legacy\" <> ''), PRIMARY KEY (\"First\", \"Second\"))");
        db.Execute("INSERT INTO \"HeldParent\" (\"Id\") VALUES (1)");
        db.Execute("INSERT INTO \"HeldChild\" (\"Tag\", \"ParentId\") VALUES ('t', 1)");
        db.Execute("INSERT INTO \"HeldPairChild\" (\"First\", \"Second\", \"ParentId\") VALUES (1, 's', 1)");

        db.Schema.Migrations()
            .Version(1, m =>
            {
                m.TableChanged<HeldChildRow>(rebuild: true);
                m.TableChanged<HeldPairChildRow>(rebuild: true);
            })
            .Migrate();

        Assert.Equal("t", db.Table<HeldChildRow>().Single().Tag);
        Assert.Equal("s", db.Table<HeldPairChildRow>().Single().Second);
    }

    [Fact]
    public void FillOnATableWithAComputedColumnRunsInTheDataPhase()
    {
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity));
        db.Schema.CreateTable<ProductLine>();
        db.Execute("INSERT INTO ProductLines (\"Id\", \"Price\", \"Quantity\") VALUES (1, 5.0, -3)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<ProductLine>(s => s.Set(x => x.Quantity, x => Math.Abs(x.Quantity))))
            .Migrate();

        Assert.Equal(3, db.Table<ProductLine>().Single().Quantity);
    }

    [Fact]
    public void FillOnANullableExistingColumnRunsInTheDataPhase()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SluggedBookRow>().Insert(new SluggedBookRow { Id = 1 }))
            .Migrate();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SluggedBookRow>().Insert(new SluggedBookRow { Id = 1 }))
            .Version(2, m => m.TableChanged<SluggedBookRow>(s => s.Set(x => x.Slug, "filled")))
            .Migrate();

        Assert.Equal("filled", db.Table<SluggedBookRow>().Single().Slug);
    }

    [Fact]
    public void FillReadingAMappedColumnThroughAMethodRunsInTheDataPhase()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<PricedBookRow>().Insert(new PricedBookRow { Id = 1, Price = -5 }))
            .Migrate();

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<PricedBookRow>().Insert(new PricedBookRow { Id = 1, Price = -5 }))
            .Version(2, m => m.TableChanged<PricedBookRow>(s => s.Set(x => x.Price, x => Math.Abs(x.Price))))
            .Migrate();

        Assert.Equal(5, db.Table<PricedBookRow>().Single().Price);
    }

    [Fact]
    public void RenamingTheContentTableOfAModelCreatedSearchTableKeepsItReadable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"OldPosts\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"OldPosts\" (\"Id\", \"Body\") VALUES (1, 'hello world')");
        db.Execute("CREATE VIRTUAL TABLE \"PostSearch\" USING fts5(\"Body\", content='OldPosts', content_rowid='Id')");
        db.Execute("INSERT INTO \"PostSearch\"(rowid, \"Body\") SELECT \"Id\", \"Body\" FROM \"OldPosts\"");

        db.Schema.Migrations()
            .Version(1, m => m.RenameTable<RenamedPostRow>("OldPosts"))
            .Migrate();

        Assert.Equal(
            "hello world",
            db.ExecuteScalar<string>("SELECT \"Body\" FROM \"PostSearch\" WHERE \"PostSearch\" MATCH 'hello'"));
    }
}
