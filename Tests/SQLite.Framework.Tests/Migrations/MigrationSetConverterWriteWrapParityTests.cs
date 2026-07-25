using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20CnvMigRows")]
public class H20CnvMigRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }

    public int? Extra { get; set; }
}

public class MigrationSetConverterWriteWrapParityTests
{
    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void MigrationSetConstantValueAppliesConverterWriteWrap(MigrateMode mode)
    {
        CwPoints five = new(5);
        using ModelTestDatabase db = new(
            model => model.Entity<H20CnvMigRow>(),
            b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Execute("CREATE TABLE \"H20CnvMigRows\" (\"Id\" INTEGER PRIMARY KEY, \"Pts\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"H20CnvMigRows\" (\"Id\", \"Pts\") VALUES (1, 1007), (2, 1009)");

        db.Table<H20CnvMigRow>().Schema.Migrate(mode, m => m.Set(x => x.Pts, five));

        List<H20CnvMigRow> simulated = [new H20CnvMigRow { Id = 1, Pts = new CwPoints(7) }, new H20CnvMigRow { Id = 2, Pts = new CwPoints(9) }];
        foreach (H20CnvMigRow row in simulated)
        {
            row.Pts = five;
        }

        List<(int Id, int N)> expected = simulated.Select(r => (r.Id, r.Pts.N)).ToList();
        List<(int Id, int N)> actual = db.Table<H20CnvMigRow>().OrderBy(r => r.Id).Select(r => new ValueTuple<int, int>(r.Id, r.Pts.N)).ToList();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void MigrationSetConstantExpressionAppliesConverterWriteWrap(MigrateMode mode)
    {
        CwPoints five = new(5);
        using ModelTestDatabase db = new(
            model => model.Entity<H20CnvMigRow>(),
            b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Execute("CREATE TABLE \"H20CnvMigRows\" (\"Id\" INTEGER PRIMARY KEY, \"Pts\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"H20CnvMigRows\" (\"Id\", \"Pts\") VALUES (1, 1007), (2, 1009)");

        db.Table<H20CnvMigRow>().Schema.Migrate(mode, m => m.Set(x => x.Pts, _ => five));

        List<H20CnvMigRow> simulated = [new H20CnvMigRow { Id = 1, Pts = new CwPoints(7) }, new H20CnvMigRow { Id = 2, Pts = new CwPoints(9) }];
        foreach (H20CnvMigRow row in simulated)
        {
            row.Pts = five;
        }

        List<(int Id, int N)> expected = simulated.Select(r => (r.Id, r.Pts.N)).ToList();
        List<(int Id, int N)> actual = db.Table<H20CnvMigRow>().OrderBy(r => r.Id).Select(r => new ValueTuple<int, int>(r.Id, r.Pts.N)).ToList();

        Assert.Equal(expected, actual);
    }
}
