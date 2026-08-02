using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[StrictTable]
[Table("MigJStrictClock")]
public class MigJStrictClockRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

[StrictTable]
[Table("MigJStrictPrice")]
public class MigJStrictPriceRow
{
    [Key]
    public int Id { get; set; }

    public decimal Amount { get; set; }
}

[Flags]
public enum MigJStrictAccess
{
    None = 0,
    Read = 1,
    Write = 2
}

[StrictTable]
[Table("MigJStrictFlags")]
public class MigJStrictFlagsRow
{
    [Key]
    public int Id { get; set; }

    public MigJStrictAccess Access { get; set; }
}

public enum MigJStrictKind
{
    Newspaper = 0,
    Magazine = 1
}

[StrictTable]
[Table("MigJStrictKind")]
public class MigJStrictKindRow
{
    [Key]
    public int Id { get; set; }

    public MigJStrictKind Kind { get; set; }
}

[StrictTable]
[Table("MigJStrictShadow")]
public class MigJStrictShadowRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class MigrationStrictStorageChangeTests
{
    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void StrictDateTimeTextColumnCopiedIntoIntegerStopsWithGuidance(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigJStrictClock\" (\"Id\" INTEGER PRIMARY KEY, \"When\" TEXT NOT NULL) STRICT");
        db.Execute("INSERT INTO \"MigJStrictClock\" (\"Id\", \"When\") VALUES (1, '2024-05-06 07:08:09')");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.Table<MigJStrictClockRow>().Schema.Migrate(mode));

        Assert.Contains("MigJStrictClock", ex.Message);
        Assert.Contains("When", ex.Message);
        Assert.Contains("Reconvert", ex.Message);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void StrictDateTimeIntegerColumnCopiedIntoTextStopsWithGuidance(MigrateMode mode)
    {
        using TestDatabase db = new(b => b.UseDateTimeStorage(DateTimeStorageMode.TextFormatted));
        db.Execute("CREATE TABLE \"MigJStrictClock\" (\"Id\" INTEGER PRIMARY KEY, \"When\" INTEGER NOT NULL) STRICT");
        db.Execute("INSERT INTO \"MigJStrictClock\" (\"Id\", \"When\") VALUES (1, 638000000000000000)");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.Table<MigJStrictClockRow>().Schema.Migrate(mode));

        Assert.Contains("MigJStrictClock", ex.Message);
        Assert.Contains("When", ex.Message);
        Assert.Contains("Reconvert", ex.Message);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void StrictDecimalTextColumnCopiedIntoRealStopsWithGuidance(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigJStrictPrice\" (\"Id\" INTEGER PRIMARY KEY, \"Amount\" TEXT NOT NULL) STRICT");
        db.Execute("INSERT INTO \"MigJStrictPrice\" (\"Id\", \"Amount\") VALUES (1, '1.50')");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.Table<MigJStrictPriceRow>().Schema.Migrate(mode));

        Assert.Contains("MigJStrictPrice", ex.Message);
        Assert.Contains("Amount", ex.Message);
        Assert.Contains("Reconvert", ex.Message);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void StrictFlagsEnumTextColumnCopiedIntoIntegerStopsWithGuidance(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigJStrictFlags\" (\"Id\" INTEGER PRIMARY KEY, \"Access\" TEXT NOT NULL) STRICT");
        db.Execute("INSERT INTO \"MigJStrictFlags\" (\"Id\", \"Access\") VALUES (1, 'Read, Write')");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.Table<MigJStrictFlagsRow>().Schema.Migrate(mode));

        Assert.Contains("MigJStrictFlags", ex.Message);
        Assert.Contains("Access", ex.Message);
        Assert.Contains("Reconvert", ex.Message);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void StrictEnumColumnMovedToTextStorageReencodesDuringTheRebuild(MigrateMode mode)
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Execute("CREATE TABLE \"MigJStrictKind\" (\"Id\" INTEGER PRIMARY KEY, \"Kind\" INTEGER NOT NULL) STRICT");
        db.Execute("INSERT INTO \"MigJStrictKind\" (\"Id\", \"Kind\") VALUES (1, 1), (2, 0)");

        db.Table<MigJStrictKindRow>().Schema.Migrate(mode);

        List<MigJStrictKind> kinds = db.Table<MigJStrictKindRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Kind)
            .ToList();

        Assert.Equal(new List<MigJStrictKind> { MigJStrictKind.Magazine, MigJStrictKind.Newspaper }, kinds);
        Assert.Equal("text", db.ExecuteScalar<string>("SELECT typeof(\"Kind\") FROM \"MigJStrictKind\" WHERE \"Id\" = 1"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void StrictEnumTextColumnReencodesToIntegerDuringTheRebuild(MigrateMode mode)
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Integer));
        db.Execute("CREATE TABLE \"MigJStrictKind\" (\"Id\" INTEGER PRIMARY KEY, \"Kind\" TEXT NOT NULL) STRICT");
        db.Execute("INSERT INTO \"MigJStrictKind\" (\"Id\", \"Kind\") VALUES (1, 'Magazine'), (2, 'Newspaper')");

        db.Table<MigJStrictKindRow>().Schema.Migrate(mode);

        List<MigJStrictKind> kinds = db.Table<MigJStrictKindRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Kind)
            .ToList();

        Assert.Equal(new List<MigJStrictKind> { MigJStrictKind.Magazine, MigJStrictKind.Newspaper }, kinds);
        Assert.Equal("integer", db.ExecuteScalar<string>("SELECT typeof(\"Kind\") FROM \"MigJStrictKind\" WHERE \"Id\" = 1"));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void StrictShadowColumnCopiedIntoTextStopsWithGuidance(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigJStrictShadowRow>()
            .Column("Meta", SQLiteColumnType.Text));
        db.Execute("CREATE TABLE \"MigJStrictShadow\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Meta\" INTEGER) STRICT");
        db.Execute("INSERT INTO \"MigJStrictShadow\" (\"Id\", \"Name\", \"Meta\") VALUES (1, 'a', 7)");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.Table<MigJStrictShadowRow>().Schema.Migrate(mode));

        Assert.Contains("MigJStrictShadow", ex.Message);
        Assert.Contains("Meta", ex.Message);
        Assert.Contains("Reconvert", ex.Message);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void StrictShadowColumnStorageChangeOnAnEmptyTableMigrates(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<MigJStrictShadowRow>()
            .Column("Meta", SQLiteColumnType.Text));
        db.Execute("CREATE TABLE \"MigJStrictShadow\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Meta\" INTEGER) STRICT");

        db.Table<MigJStrictShadowRow>().Schema.Migrate(mode);

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"MigJStrictShadow\""));
    }
}
