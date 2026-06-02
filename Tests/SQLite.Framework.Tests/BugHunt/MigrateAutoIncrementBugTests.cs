using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

[Table("AiMigrate")]
file sealed class AiMigrateRow
{
    [Key, AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class MigrateAutoIncrementBugTests
{
    [Fact]
    public void MigrateSet_RebuildKeepsAutoIncrementHighWaterMark()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<AiMigrateRow>();
        db.Table<AiMigrateRow>().Add(new AiMigrateRow { Name = "a" });
        db.Table<AiMigrateRow>().Add(new AiMigrateRow { Name = "b" });
        db.Table<AiMigrateRow>().Add(new AiMigrateRow { Name = "c" });
        db.Execute("DELETE FROM \"AiMigrate\" WHERE \"Id\" = 3");

        db.Schema.Table<AiMigrateRow>().Migrate(m => m.Set(x => x.Name, x => x.Name));

        AiMigrateRow inserted = new() { Name = "d" };
        db.Table<AiMigrateRow>().Add(inserted);

        Assert.Equal(4, inserted.Id);
    }

    [Fact]
    public void Migrate_DriftRebuildKeepsAutoIncrementHighWaterMark()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"AiMigrate\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NOT NULL, \"Old\" TEXT)");
        db.Execute("INSERT INTO \"AiMigrate\" (\"Name\") VALUES ('a'), ('b'), ('c')");
        db.Execute("DELETE FROM \"AiMigrate\" WHERE \"Id\" = 3");

        db.Schema.Table<AiMigrateRow>().Migrate();

        db.Execute("INSERT INTO \"AiMigrate\" (\"Name\") VALUES ('d')");
        long newId = db.ExecuteScalar<long>("SELECT \"Id\" FROM \"AiMigrate\" WHERE \"Name\" = 'd'");

        Assert.Equal(4, newId);
    }
}
