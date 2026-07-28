using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23dDefaultedNotes")]
public class H23dDefaultedNote
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Extra { get; set; }
}

[Table("H23dRequiredNotes")]
public class H23dRequiredNote
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Extra { get; set; } = "";
}

[Table("H23dShadowNotes")]
public class H23dShadowNote
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H23dMappedNotes")]
public class H23dMappedNote
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Extra { get; set; }
}

public class DeferredRebuildAddedColumnDeclarationTests
{
    [Fact]
    public void AColumnAddedBeforeADeferredRebuildKeepsItsDeclaredDefault()
    {
        using ModelTestDatabase withEarlierStep = new(model => model.Entity<H23dDefaultedNote>().Default(r => r.Extra, "d"));
        Seed(withEarlierStep, "H23dDefaultedNotes");
        withEarlierStep.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H23dDefaultedNote>(s => s.Set(r => r.Name, r => (r.Name == null ? "seed" : r.Name) + "-filled")))
            .Migrate();

        using ModelTestDatabase withoutEarlierStep = new(model => model.Entity<H23dDefaultedNote>().Default(r => r.Extra, "d"));
        Seed(withoutEarlierStep, "H23dDefaultedNotes");
        withoutEarlierStep.Schema.Migrations()
            .Version(2, m => m.TableChanged<H23dDefaultedNote>(s => s.Set(r => r.Name, r => (r.Name == null ? "seed" : r.Name) + "-filled")))
            .Migrate();

        List<string?> withoutEarlier = withoutEarlierStep.Table<H23dDefaultedNote>().OrderBy(r => r.Id).Select(r => r.Extra).ToList();
        List<string?> withEarlier = withEarlierStep.Table<H23dDefaultedNote>().OrderBy(r => r.Id).Select(r => r.Extra).ToList();

        Assert.Equal(["d", "d"], withoutEarlier);
        Assert.Equal(withoutEarlier, withEarlier);
    }

    [Fact]
    public void ARequiredColumnWithADeclaredDefaultIsAddedBeforeTheDeferredRebuild()
    {
        using ModelTestDatabase db = new(model => model.Entity<H23dRequiredNote>().Default(r => r.Extra, "d"));
        Seed(db, "H23dRequiredNotes");

        db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H23dRequiredNote>(s => s.Set(r => r.Name, r => (r.Name == null ? "seed" : r.Name) + "-filled")))
            .Migrate();

        Assert.Equal(["d", "d"], db.Table<H23dRequiredNote>().OrderBy(r => r.Id).Select(r => r.Extra).ToList());
    }

    [Fact]
    public void AnUnfilledRequiredColumnFailsTheSameWayWithAndWithoutAnEarlierRawStep()
    {
        using TestDatabase withEarlierStep = new(useFile: true);
        Seed(withEarlierStep, "H23dRequiredNotes");
        Exception? withEarlier = Record.Exception(() => withEarlierStep.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m.TableChanged<H23dRequiredNote>(s => s.Set(r => r.Name, r => (r.Name == null ? "seed" : r.Name) + "-filled")))
            .Migrate());

        using TestDatabase withoutEarlierStep = new(useFile: true);
        Seed(withoutEarlierStep, "H23dRequiredNotes");
        Exception? withoutEarlier = Record.Exception(() => withoutEarlierStep.Schema.Migrations()
            .Version(2, m => m.TableChanged<H23dRequiredNote>(s => s.Set(r => r.Name, r => (r.Name == null ? "seed" : r.Name) + "-filled")))
            .Migrate());

        Assert.IsType<InvalidOperationException>(withoutEarlier);
        Assert.Equal(withoutEarlier?.GetType(), withEarlier?.GetType());
    }

    [Fact]
    public void AMappedColumnFillRunsBeforeTheDeferredRebuild()
    {
        using TestDatabase db = new(useFile: true);
        Seed(db, "H23dMappedNotes");

        db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m
                .TableChanged<H23dMappedNote>(s => s.Set(r => r.Extra, "seed"))
                .Delete<H23dMappedNote>(r => r.Id == 999))
            .Version(3, m => m.TableChanged<H23dMappedNote>(s => s.Set(r => r.Name, r => (r.Name == null ? "seed" : r.Name) + "-filled")))
            .Migrate();

        Assert.Equal(["seed", "seed"], db.Query<string>("SELECT \"Extra\" FROM \"H23dMappedNotes\" ORDER BY \"Id\""));
    }

    [Fact]
    public void ADeclaredShadowColumnFillRunsBeforeTheDeferredRebuild()
    {
        using ModelTestDatabase db = new(model => model.Entity<H23dShadowNote>()
            .Column("Extra", SQLiteColumnType.Text, nullable: true));
        Seed(db, "H23dShadowNotes");

        db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1"))
            .Version(2, m => m
                .TableChanged<H23dShadowNote>(s => s.Set(r => SQLiteColumn.Of<string?>(r, "Extra"), "seed"))
                .Delete<H23dShadowNote>(r => r.Id == 999))
            .Version(3, m => m.TableChanged<H23dShadowNote>(s => s.Set(r => r.Name, r => (r.Name == null ? "seed" : r.Name) + "-filled")))
            .Migrate();

        Assert.Equal(["seed", "seed"], db.Query<string>("SELECT \"Extra\" FROM \"H23dShadowNotes\" ORDER BY \"Id\""));
    }

    private static void Seed(TestDatabase db, string table)
    {
        db.Execute($"CREATE TABLE \"{table}\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)");
        db.Execute($"INSERT INTO \"{table}\" (\"Id\", \"Name\") VALUES (1, NULL), (2, 'b')");
    }
}
