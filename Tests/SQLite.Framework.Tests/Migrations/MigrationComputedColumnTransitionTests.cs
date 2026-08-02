using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigA_ComputedShift")]
public class MigAComputedShiftRow
{
    [Key]
    public int Id { get; set; }

    public int Base { get; set; }

    public int? Doubled { get; set; }
}

[Table("MigA_ComputedDrop")]
public class MigAComputedDropRow
{
    [Key]
    public int Id { get; set; }

    public int Base { get; set; }
}

public class MigrationComputedColumnTransitionTests
{
    [Fact]
    public void ComputedColumnTurnedRegularKeepsItsValuesOnRebuild()
    {
        using TestDatabase db = new(useFile: true);
        SeedShift(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<MigAComputedShiftRow>(rebuild: true))
            .Migrate();

        List<int?> values = db.Table<MigAComputedShiftRow>()
            .OrderBy(x => x.Id)
            .Select(x => x.Doubled)
            .ToList();
        Assert.Equal(new List<int?> { 42, 8 }, values);
    }

    [Fact]
    public void ComputedColumnTurnedRegularKeepsItsValuesInPlace()
    {
        using TestDatabase db = new(useFile: true);
        SeedShift(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<MigAComputedShiftRow>())
            .Migrate();

        List<int?> values = db.Table<MigAComputedShiftRow>()
            .OrderBy(x => x.Id)
            .Select(x => x.Doubled)
            .ToList();
        Assert.Equal(new List<int?> { 42, 8 }, values);
    }

    [Fact]
    public void DropColumnStepDropsAVirtualComputedColumn()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"MigA_ComputedDrop\" (\"Id\" INTEGER PRIMARY KEY, \"Base\" INTEGER NOT NULL, \"Doubled\" INTEGER GENERATED ALWAYS AS (\"Base\" * 2) VIRTUAL)");
        db.Execute("INSERT INTO \"MigA_ComputedDrop\" (\"Id\", \"Base\") VALUES (1, 21)");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<MigAComputedDropRow>("Doubled"))
            .Migrate();

        Assert.False(db.Schema.ColumnExists<MigAComputedDropRow>("Doubled"));
        Assert.Equal(21, db.Table<MigAComputedDropRow>().Single().Base);
    }

    private static void SeedShift(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"MigA_ComputedShift\" (\"Id\" INTEGER PRIMARY KEY, \"Base\" INTEGER NOT NULL, \"Doubled\" INTEGER GENERATED ALWAYS AS (\"Base\" * 2) VIRTUAL)");
        db.Execute("INSERT INTO \"MigA_ComputedShift\" (\"Id\", \"Base\") VALUES (1, 21), (2, 4)");
    }
}
