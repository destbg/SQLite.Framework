using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct H24qMigPoints
{
    public H24qMigPoints(int n)
    {
        N = n;
    }

    public int N { get; }

    public static bool operator ==(H24qMigPoints a, H24qMigPoints b) => a.N == b.N;

    public static bool operator !=(H24qMigPoints a, H24qMigPoints b) => a.N != b.N;

    public override bool Equals(object? obj) => obj is H24qMigPoints p && p.N == N;

    public override int GetHashCode() => N;
}

public sealed class H24qMigPointsConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value) => value is H24qMigPoints p ? (long)p.N : null;

    public object? FromDatabase(object? value) => value is long l ? new H24qMigPoints((int)l) : new H24qMigPoints(0);
}

[Table("H24qMigSetRows")]
public class H24qMigSetRow
{
    [Key]
    public int Id { get; set; }

    public bool Keep { get; set; }

    public H24qMigPoints Pts { get; set; }
}

public class MigrationSetComputedValueConverterWriteWrapTests
{
    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void MigrationSetOverAConditionalValueAppliesConverterWriteWrap(MigrateMode mode)
    {
        H24qMigPoints five = new(5);
        using ModelTestDatabase db = new(
            model => model.Entity<H24qMigSetRow>(),
            b => b.AddTypeConverter<H24qMigPoints>(new H24qMigPointsConverter()));
        db.Execute("CREATE TABLE \"H24qMigSetRows\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" INTEGER NOT NULL, \"Pts\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"H24qMigSetRows\" (\"Id\", \"Keep\", \"Pts\") VALUES (1, 1, 1007), (2, 0, 1009)");

        db.Table<H24qMigSetRow>().Schema.Migrate(mode, m => m.Set(x => x.Pts, x => x.Keep ? x.Pts : five));

        List<H24qMigSetRow> simulated =
        [
            new H24qMigSetRow { Id = 1, Keep = true, Pts = new H24qMigPoints(7) },
            new H24qMigSetRow { Id = 2, Keep = false, Pts = new H24qMigPoints(9) }
        ];
        foreach (H24qMigSetRow row in simulated)
        {
            row.Pts = row.Keep ? row.Pts : five;
        }

        List<int> expected = simulated.OrderBy(r => r.Id).Select(r => r.Pts.N).ToList();
        List<int> actual = db.Table<H24qMigSetRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Pts)
            .ToList()
            .Select(p => p.N)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
