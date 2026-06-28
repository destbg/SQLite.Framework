using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("IpChk")]
public sealed class IpChkRow
{
    [Key]
    public int Id { get; set; }
}

[Table("IpLit")]
public sealed class IpLitRow
{
    [Key]
    public int Id { get; set; }

    public required string Note { get; set; }
}

public class MigrateInPlaceCheckDropParityTests
{
    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void Migrate_DropsColumnInUnquotedTableLevelCheck(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"IpChk\" (\"Id\" INTEGER PRIMARY KEY, \"Flag\" INTEGER, CHECK (Flag IN (0, 1)))");
        db.Execute("INSERT INTO \"IpChk\" (\"Id\", \"Flag\") VALUES (1, 1)");

        db.Schema.Table<IpChkRow>().Migrate(mode);

        Assert.False(db.Schema.Table<IpChkRow>().ColumnExists("Flag"));
        Assert.Equal(1, db.Table<IpChkRow>().Count());
    }

    [Fact]
    public void Migrate_InPlace_DropsColumnWhenTableHasStringLiteralDefault()
    {
        using ModelTestDatabase db = new(model => model.Entity<IpLitRow>().Default(r => r.Note, "hi"));
        db.Execute("CREATE TABLE \"IpLit\" (\"Id\" INTEGER PRIMARY KEY, \"Note\" TEXT NOT NULL DEFAULT 'hi', \"Old\" TEXT)");
        db.Execute("INSERT INTO \"IpLit\" (\"Id\", \"Note\", \"Old\") VALUES (1, 'keep', 'gone')");

        db.Schema.Table<IpLitRow>().Migrate(MigrateMode.InPlace);

        Assert.False(db.Schema.Table<IpLitRow>().ColumnExists("Old"));
        Assert.Equal("keep", db.Table<IpLitRow>().Single().Note);
    }
}
