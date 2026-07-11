using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("PhasedFillLedger")]
public class PhasedFillLedgerRow
{
    [Key]
    public int Id { get; set; }

    public int Required { get; set; }

    public int Source { get; set; }
}

[Table("DerivedExtraLedger")]
public class DerivedExtraLedgerRow
{
    [Key]
    public int Id { get; set; }

    public int Source { get; set; }

    public int? Extra { get; set; }
}

[Table("RunThenFillLedger")]
public class RunThenFillLedgerRow
{
    [Key]
    public int Id { get; set; }

    public int Source { get; set; }

    public int Derived { get; set; }
}

[Table("SeedThenFillLedger")]
public class SeedThenFillLedgerRow
{
    [Key]
    public int Id { get; set; }

    public int Base { get; set; }

    public int Doubled { get; set; }
}

public class MigrationCrossVersionFillReadOrderTests
{
    [Fact]
    public void RequiredColumnDerivedFromAnEarlierFilledColumnMatchesStepwise()
    {
        using TestDatabase collapsed = new(useFile: true);
        collapsed.Execute("CREATE TABLE \"PhasedFillLedger\" (\"Id\" INTEGER PRIMARY KEY, \"Required\" INTEGER, \"Source\" INTEGER NOT NULL)");
        collapsed.Execute("INSERT INTO \"PhasedFillLedger\" (\"Id\", \"Required\", \"Source\") VALUES (1, 5, 100)");
        collapsed.Schema.Migrations()
            .Version(2, m => m.TableChanged<PhasedFillLedgerRow>(s => s.Set(x => x.Source, 999)))
            .Version(3, m => m.TableChanged<PhasedFillLedgerRow>(s => s.Set(x => x.Required, r => r.Source)))
            .Migrate();

        using TestDatabase stepwise = new(useFile: true);
        stepwise.Execute("CREATE TABLE \"PhasedFillLedger\" (\"Id\" INTEGER PRIMARY KEY, \"Required\" INTEGER, \"Source\" INTEGER NOT NULL)");
        stepwise.Execute("INSERT INTO \"PhasedFillLedger\" (\"Id\", \"Required\", \"Source\") VALUES (1, 5, 100)");
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<PhasedFillLedgerRow>(s => s.Set(x => x.Source, 999)))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<PhasedFillLedgerRow>(s => s.Set(x => x.Source, 999)))
            .Version(3, m => m.TableChanged<PhasedFillLedgerRow>(s => s.Set(x => x.Required, r => r.Source)))
            .Migrate();

        int stepwiseRequired = stepwise.Table<PhasedFillLedgerRow>().Single().Required;
        int collapsedRequired = collapsed.Table<PhasedFillLedgerRow>().Single().Required;

        Assert.Equal(999, stepwiseRequired);
        Assert.Equal(stepwiseRequired, collapsedRequired);
    }

    [Fact]
    public void NewNullableColumnDerivedFromAnEarlierFilledColumnMatchesStepwise()
    {
        using TestDatabase collapsed = new(useFile: true);
        collapsed.Execute("CREATE TABLE \"DerivedExtraLedger\" (\"Id\" INTEGER PRIMARY KEY, \"Source\" INTEGER NOT NULL)");
        collapsed.Execute("INSERT INTO \"DerivedExtraLedger\" (\"Id\", \"Source\") VALUES (1, 100)");
        collapsed.Schema.Migrations()
            .Version(2, m => m.TableChanged<DerivedExtraLedgerRow>(s => s.Set(x => x.Source, 999)))
            .Version(3, m => m.TableChanged<DerivedExtraLedgerRow>(s => s.Set(x => x.Extra, r => SQLiteColumn.Of<int?>(r, "Source"))))
            .Migrate();

        using TestDatabase stepwise = new(useFile: true);
        stepwise.Execute("CREATE TABLE \"DerivedExtraLedger\" (\"Id\" INTEGER PRIMARY KEY, \"Source\" INTEGER NOT NULL)");
        stepwise.Execute("INSERT INTO \"DerivedExtraLedger\" (\"Id\", \"Source\") VALUES (1, 100)");
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<DerivedExtraLedgerRow>(s => s.Set(x => x.Source, 999)))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.TableChanged<DerivedExtraLedgerRow>(s => s.Set(x => x.Source, 999)))
            .Version(3, m => m.TableChanged<DerivedExtraLedgerRow>(s => s.Set(x => x.Extra, r => SQLiteColumn.Of<int?>(r, "Source"))))
            .Migrate();

        int? stepwiseExtra = stepwise.Table<DerivedExtraLedgerRow>().Single().Extra;
        int? collapsedExtra = collapsed.Table<DerivedExtraLedgerRow>().Single().Extra;

        Assert.Equal(999, stepwiseExtra);
        Assert.Equal(stepwiseExtra, collapsedExtra);
    }

    [Fact]
    public void SchemaFillReadingAColumnAnEarlierRunCallbackWroteMatchesStepwise()
    {
        using TestDatabase collapsed = new(useFile: true);
        collapsed.Execute("CREATE TABLE \"RunThenFillLedger\" (\"Id\" INTEGER PRIMARY KEY, \"Source\" INTEGER NOT NULL, \"Derived\" INTEGER)");
        collapsed.Execute("INSERT INTO \"RunThenFillLedger\" (\"Id\", \"Source\") VALUES (1, 7)");
        collapsed.Schema.Migrations()
            .Version(2, m => m.Run(ctx => ctx.Database.Execute("UPDATE \"RunThenFillLedger\" SET \"Source\" = \"Source\" * 10")))
            .Version(3, m => m.TableChanged<RunThenFillLedgerRow>(s => s.Set(x => x.Derived, r => r.Source)))
            .Migrate();

        using TestDatabase stepwise = new(useFile: true);
        stepwise.Execute("CREATE TABLE \"RunThenFillLedger\" (\"Id\" INTEGER PRIMARY KEY, \"Source\" INTEGER NOT NULL, \"Derived\" INTEGER)");
        stepwise.Execute("INSERT INTO \"RunThenFillLedger\" (\"Id\", \"Source\") VALUES (1, 7)");
        stepwise.Schema.Migrations()
            .Version(2, m => m.Run(ctx => ctx.Database.Execute("UPDATE \"RunThenFillLedger\" SET \"Source\" = \"Source\" * 10")))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.Run(ctx => ctx.Database.Execute("UPDATE \"RunThenFillLedger\" SET \"Source\" = \"Source\" * 10")))
            .Version(3, m => m.TableChanged<RunThenFillLedgerRow>(s => s.Set(x => x.Derived, r => r.Source)))
            .Migrate();

        int stepwiseDerived = stepwise.Table<RunThenFillLedgerRow>().Single().Derived;
        int collapsedDerived = collapsed.Table<RunThenFillLedgerRow>().Single().Derived;

        Assert.Equal(70, stepwiseDerived);
        Assert.Equal(stepwiseDerived, collapsedDerived);
    }

    [Fact]
    public void SchemaFillReachesAnEarlierSeededRowLikeStepwise()
    {
        using TestDatabase collapsed = new(useFile: true);
        collapsed.Execute("CREATE TABLE \"SeedThenFillLedger\" (\"Id\" INTEGER PRIMARY KEY, \"Base\" INTEGER NOT NULL, \"Doubled\" INTEGER)");
        collapsed.Execute("INSERT INTO \"SeedThenFillLedger\" (\"Id\", \"Base\") VALUES (1, 10)");
        collapsed.Schema.Migrations()
            .Version(2, m => m.Insert(new SeedThenFillLedgerRow { Id = 2, Base = 5, Doubled = 999 }))
            .Version(3, m => m.TableChanged<SeedThenFillLedgerRow>(s => s.Set(x => x.Doubled, r => r.Base * 2)))
            .Migrate();

        using TestDatabase stepwise = new(useFile: true);
        stepwise.Execute("CREATE TABLE \"SeedThenFillLedger\" (\"Id\" INTEGER PRIMARY KEY, \"Base\" INTEGER NOT NULL, \"Doubled\" INTEGER)");
        stepwise.Execute("INSERT INTO \"SeedThenFillLedger\" (\"Id\", \"Base\") VALUES (1, 10)");
        stepwise.Schema.Migrations()
            .Version(2, m => m.Insert(new SeedThenFillLedgerRow { Id = 2, Base = 5, Doubled = 999 }))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.Insert(new SeedThenFillLedgerRow { Id = 2, Base = 5, Doubled = 999 }))
            .Version(3, m => m.TableChanged<SeedThenFillLedgerRow>(s => s.Set(x => x.Doubled, r => r.Base * 2)))
            .Migrate();

        int stepwiseSeed = stepwise.Table<SeedThenFillLedgerRow>().Single(r => r.Id == 2).Doubled;
        int collapsedSeed = collapsed.Table<SeedThenFillLedgerRow>().Single(r => r.Id == 2).Doubled;

        Assert.Equal(10, stepwiseSeed);
        Assert.Equal(stepwiseSeed, collapsedSeed);
    }
}
