using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("migord_OwnFillTally")]
public class MigOrdOwnFillTallyRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }
}

[Table("migord_OwnFillSeed")]
public class MigOrdOwnFillSeedRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }
}

#if !SQLITECIPHER
[Table("migord_OwnFillJson")]
public class MigOrdOwnFillJsonRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}
#endif

public class MigrationOwnColumnFillOrderParityTests
{
    [Fact]
    public void RunCallbackBeforeAnOwnColumnFillMatchesStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        stepwise.Execute("CREATE TABLE \"migord_OwnFillTally\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
        stepwise.Execute("INSERT INTO \"migord_OwnFillTally\" (\"Id\", \"Val\") VALUES (1, 10)");
        stepwise.Pragmas.UserVersion = 1;
        stepwise.Schema.Migrations()
            .Version(2, m => m.Run(ctx => ctx.Database.Execute("UPDATE \"migord_OwnFillTally\" SET \"Val\" = \"Val\" * 100")))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.Run(ctx => ctx.Database.Execute("UPDATE \"migord_OwnFillTally\" SET \"Val\" = \"Val\" * 100")))
            .Version(3, m => m.TableChanged<MigOrdOwnFillTallyRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        collapsed.Execute("CREATE TABLE \"migord_OwnFillTally\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
        collapsed.Execute("INSERT INTO \"migord_OwnFillTally\" (\"Id\", \"Val\") VALUES (1, 10)");
        collapsed.Pragmas.UserVersion = 1;
        collapsed.Schema.Migrations()
            .Version(2, m => m.Run(ctx => ctx.Database.Execute("UPDATE \"migord_OwnFillTally\" SET \"Val\" = \"Val\" * 100")))
            .Version(3, m => m.TableChanged<MigOrdOwnFillTallyRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        int stepwiseVal = stepwise.Table<MigOrdOwnFillTallyRow>().Single().Val;
        int collapsedVal = collapsed.Table<MigOrdOwnFillTallyRow>().Single().Val;

        Assert.Equal(1001, stepwiseVal);
        Assert.Equal(stepwiseVal, collapsedVal);
    }

    [Fact]
    public void SeedInsertBeforeAnOwnColumnFillMatchesStepwise()
    {
        using TestDatabase stepwise = new(useFile: true);
        stepwise.Execute("CREATE TABLE \"migord_OwnFillSeed\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
        stepwise.Execute("INSERT INTO \"migord_OwnFillSeed\" (\"Id\", \"Val\") VALUES (1, 10)");
        stepwise.Pragmas.UserVersion = 1;
        stepwise.Schema.Migrations()
            .Version(2, m => m.Insert(new MigOrdOwnFillSeedRow { Id = 2, Val = 50 }))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.Insert(new MigOrdOwnFillSeedRow { Id = 2, Val = 50 }))
            .Version(3, m => m.TableChanged<MigOrdOwnFillSeedRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        using TestDatabase collapsed = new(useFile: true);
        collapsed.Execute("CREATE TABLE \"migord_OwnFillSeed\" (\"Id\" INTEGER PRIMARY KEY, \"Val\" INTEGER)");
        collapsed.Execute("INSERT INTO \"migord_OwnFillSeed\" (\"Id\", \"Val\") VALUES (1, 10)");
        collapsed.Pragmas.UserVersion = 1;
        collapsed.Schema.Migrations()
            .Version(2, m => m.Insert(new MigOrdOwnFillSeedRow { Id = 2, Val = 50 }))
            .Version(3, m => m.TableChanged<MigOrdOwnFillSeedRow>(s => s.Set(x => x.Val, r => r.Val + 1)))
            .Migrate();

        List<int> stepwiseVals = stepwise.Table<MigOrdOwnFillSeedRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();
        List<int> collapsedVals = collapsed.Table<MigOrdOwnFillSeedRow>().OrderBy(x => x.Id).Select(x => x.Val).ToList();

        Assert.Equal([11, 51], stepwiseVals);
        Assert.Equal(stepwiseVals, collapsedVals);
    }

#if !SQLITECIPHER
    [Fact]
    public void RawWriteBeforeAReconvertMatchesStepwise()
    {
        using TestDatabase stepwise = new(
            b => b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address),
            useFile: true);
        stepwise.Execute("CREATE TABLE \"migord_OwnFillJson\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" BLOB NOT NULL)");
        stepwise.Execute("INSERT INTO \"migord_OwnFillJson\" (\"Id\", \"Data\") VALUES (1, '{\"Street\":\"old\",\"City\":\"o\"}')");
        stepwise.Pragmas.UserVersion = 1;
        stepwise.Schema.Migrations()
            .Version(2, m => m.Sql("UPDATE \"migord_OwnFillJson\" SET \"Data\" = '{\"Street\":\"raw\",\"City\":\"r\"}'"))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m.Sql("UPDATE \"migord_OwnFillJson\" SET \"Data\" = '{\"Street\":\"raw\",\"City\":\"r\"}'"))
            .Version(3, m => m.TableChanged<MigOrdOwnFillJsonRow>(s => s.Reconvert(x => x.Data)))
            .Migrate();

        using TestDatabase collapsed = new(
            b => b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address),
            useFile: true);
        collapsed.Execute("CREATE TABLE \"migord_OwnFillJson\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" BLOB NOT NULL)");
        collapsed.Execute("INSERT INTO \"migord_OwnFillJson\" (\"Id\", \"Data\") VALUES (1, '{\"Street\":\"old\",\"City\":\"o\"}')");
        collapsed.Pragmas.UserVersion = 1;
        collapsed.Schema.Migrations()
            .Version(2, m => m.Sql("UPDATE \"migord_OwnFillJson\" SET \"Data\" = '{\"Street\":\"raw\",\"City\":\"r\"}'"))
            .Version(3, m => m.TableChanged<MigOrdOwnFillJsonRow>(s => s.Reconvert(x => x.Data)))
            .Migrate();

        string? stepwiseType = stepwise.ExecuteScalar<string?>("SELECT typeof(\"Data\") FROM \"migord_OwnFillJson\"");
        string? collapsedType = collapsed.ExecuteScalar<string?>("SELECT typeof(\"Data\") FROM \"migord_OwnFillJson\"");

        Assert.Equal("blob", stepwiseType);
        Assert.Equal(stepwiseType, collapsedType);
    }
#endif
}
