using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigDropPk")]
file sealed class MigDropPk
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

[Table("MigDropChk")]
file sealed class MigDropChk
{
    [Key]
    public int Id { get; set; }
}

[Table("MigDropFkParent")]
file sealed class MigDropFkParent
{
    [Key]
    public int Id { get; set; }
}

[Table("MigDropFk")]
file sealed class MigDropFk
{
    [Key]
    public int Id { get; set; }
}

public class MigrateAlterDropGuardTests
{
    [Fact]
    public void MigrateDropsPrimaryKeyColumnNotInModelByRebuilding()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigDropPk\" (\"LegacyId\" INTEGER PRIMARY KEY, \"Value\" INTEGER)");

        db.Schema.Table<MigDropPk>().Migrate();

        Assert.True(db.Schema.Table<MigDropPk>().ColumnExists("Id"));
        Assert.False(db.Schema.Table<MigDropPk>().ColumnExists("LegacyId"));
        Assert.True(db.Schema.Table<MigDropPk>().ColumnExists("Value"));
    }

    [Fact]
    public void MigrateDropsColumnInTableLevelCheckByRebuilding()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigDropChk\" (\"Id\" INTEGER PRIMARY KEY, \"Flag\" INTEGER, CHECK (\"Flag\" IN (0, 1)))");
        db.Execute("INSERT INTO \"MigDropChk\" (\"Id\", \"Flag\") VALUES (1, 1)");

        db.Schema.Table<MigDropChk>().Migrate();

        Assert.False(db.Schema.Table<MigDropChk>().ColumnExists("Flag"));
        Assert.Equal(1, db.Table<MigDropChk>().Count());
    }

    [Fact]
    public void MigrateDropsForeignKeySourceColumnByRebuilding()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigDropFkParent\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"MigDropFk\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER REFERENCES \"MigDropFkParent\"(\"Id\"))");
        db.Execute("INSERT INTO \"MigDropFkParent\" (\"Id\") VALUES (1)");
        db.Execute("INSERT INTO \"MigDropFk\" (\"Id\", \"ParentId\") VALUES (1, 1)");

        db.Schema.Table<MigDropFk>().Migrate();

        Assert.False(db.Schema.Table<MigDropFk>().ColumnExists("ParentId"));
        Assert.Equal(1, db.Table<MigDropFk>().Count());
    }
}
