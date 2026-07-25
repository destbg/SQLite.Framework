#if !SQLITECIPHER
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[StrictTable]
internal sealed class ReconvertStrictRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

internal sealed class ReconvertPlainRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

public class MigrateReconvertTests
{
    private static TestDatabase Db()
    {
        return new TestDatabase(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Reconvert_RewritesJsonTextAsJsonbUnderStrict(MigrateMode mode)
    {
        using TestDatabase db = Db();
        db.Execute("CREATE TABLE \"ReconvertStrictRow\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" TEXT NOT NULL) STRICT");
        db.Execute("INSERT INTO \"ReconvertStrictRow\" (\"Id\", \"Data\") VALUES (1, '{\"Street\":\"1\",\"City\":\"A\"}')");

        db.Table<ReconvertStrictRow>().Schema.Migrate(mode, m => m.Reconvert(x => x.Data));

        Assert.Equal("blob", db.ExecuteScalar<string>("SELECT typeof(\"Data\") FROM \"ReconvertStrictRow\""));
        Address data = db.Table<ReconvertStrictRow>().Select(r => r.Data).Single();
        Assert.Equal(("1", "A"), (data.Street, data.City));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_Reconvert_RewritesJsonTextAsJsonbOnPlainTable(MigrateMode mode)
    {
        using TestDatabase db = Db();
        db.Execute("CREATE TABLE \"ReconvertPlainRow\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"ReconvertPlainRow\" (\"Id\", \"Data\") VALUES (1, '{\"Street\":\"9\",\"City\":\"Z\"}')");

        db.Table<ReconvertPlainRow>().Schema.Migrate(mode, m => m.Reconvert(x => x.Data));

        Assert.Equal("blob", db.ExecuteScalar<string>("SELECT typeof(\"Data\") FROM \"ReconvertPlainRow\""));
        Address data = db.Table<ReconvertPlainRow>().Select(r => r.Data).Single();
        Assert.Equal(("9", "Z"), (data.Street, data.City));
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_StrictStorageClassChange_WithoutConversion_ThrowsGuidance(MigrateMode mode)
    {
        using TestDatabase db = Db();
        db.Execute("CREATE TABLE \"ReconvertStrictRow\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" TEXT NOT NULL) STRICT");
        db.Execute("INSERT INTO \"ReconvertStrictRow\" (\"Id\", \"Data\") VALUES (1, '{\"Street\":\"1\",\"City\":\"A\"}')");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.Table<ReconvertStrictRow>().Schema.Migrate(mode));

        Assert.Contains("Data", ex.Message);
        Assert.Contains("Reconvert", ex.Message);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_StrictStorageClassChange_EmptyTable_Succeeds(MigrateMode mode)
    {
        using TestDatabase db = Db();
        db.Execute("CREATE TABLE \"ReconvertStrictRow\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" TEXT NOT NULL) STRICT");

        db.Table<ReconvertStrictRow>().Schema.Migrate(mode);

        string? sql = db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ReconvertStrictRow'");
        Assert.Contains("BLOB", sql);
    }
}
#endif
