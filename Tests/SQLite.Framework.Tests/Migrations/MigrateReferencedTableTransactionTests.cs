using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("TxMigParent")]
file sealed class TxMigParent
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class MigrateReferencedTableTransactionTests
{
    private static void SeedReferenced(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"TxMigParent\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Old\" TEXT)");
        db.Execute("CREATE TABLE \"TxMigChild\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER NOT NULL REFERENCES \"TxMigParent\"(\"Id\"))");
        db.Execute("INSERT INTO \"TxMigParent\" (\"Id\", \"Name\") VALUES (1, 'p')");
        db.Execute("INSERT INTO \"TxMigChild\" (\"Id\", \"ParentId\") VALUES (1, 1)");
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void MigrateReferencedParentOutsideTransactionPreservesChild(MigrateMode mode)
    {
        using TestDatabase db = new();
        SeedReferenced(db);

        db.Schema.Table<TxMigParent>().Migrate(mode);

        Assert.Equal(1, db.Query<int>("SELECT \"ParentId\" FROM \"TxMigChild\"").First());
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void MigrateReferencedParentInsideTransactionPreservesChild(MigrateMode mode)
    {
        using TestDatabase db = new();
        SeedReferenced(db);

        using SQLiteTransaction tx = db.BeginTransaction();
        db.Schema.Table<TxMigParent>().Migrate(mode);
        tx.Commit();

        Assert.Equal(1, db.Query<int>("SELECT \"ParentId\" FROM \"TxMigChild\"").First());
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void MigrateReferencedParentWithMultiLevelIndexedChildrenInsideTransactionPreservesData(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"TxMigParent\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Old\" TEXT)");
        db.Execute("CREATE TABLE \"TxMigChild\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"ParentId\" INTEGER NOT NULL REFERENCES \"TxMigParent\"(\"Id\"), \"Tag\" TEXT)");
        db.Execute("CREATE TABLE \"TxMigGrandchild\" (\"Id\" INTEGER PRIMARY KEY, \"ChildId\" INTEGER NOT NULL REFERENCES \"TxMigChild\"(\"Id\"))");
        db.Execute("CREATE INDEX \"ix_child_tag\" ON \"TxMigChild\" (\"Tag\")");
        db.Execute("CREATE TRIGGER \"trg_child\" AFTER INSERT ON \"TxMigChild\" BEGIN UPDATE \"TxMigParent\" SET \"Name\" = \"Name\"; END");
        db.Execute("CREATE TABLE \"TxMigOtherTarget\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"TxMigSibling\" (\"Id\" INTEGER PRIMARY KEY, \"OtherId\" INTEGER REFERENCES \"TxMigOtherTarget\"(\"Id\"))");
        db.Execute("INSERT INTO \"TxMigParent\" (\"Id\", \"Name\") VALUES (1, 'p')");
        db.Execute("INSERT INTO \"TxMigChild\" (\"ParentId\", \"Tag\") VALUES (1, 'a'), (1, 'b'), (1, 'c')");
        db.Execute("DELETE FROM \"TxMigChild\" WHERE \"Id\" = 3");
        db.Execute("INSERT INTO \"TxMigGrandchild\" (\"Id\", \"ChildId\") VALUES (1, 1)");

        using SQLiteTransaction tx = db.BeginTransaction();
        db.Schema.Table<TxMigParent>().Migrate(mode);
        tx.Commit();

        Assert.Equal([1, 2], db.Query<int>("SELECT \"Id\" FROM \"TxMigChild\" ORDER BY \"Id\"").ToList());
        Assert.Equal("a", db.Query<string>("SELECT \"Tag\" FROM \"TxMigChild\" WHERE \"Id\" = 1").First());
        Assert.Equal(3, db.Query<int>("SELECT \"seq\" FROM sqlite_sequence WHERE \"name\" = 'TxMigChild'").First());
        Assert.Equal(1, db.Query<int>("SELECT \"ChildId\" FROM \"TxMigGrandchild\"").First());
        Assert.Equal(1, db.Query<int>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'ix_child_tag'").First());
        Assert.Equal(1, db.Query<int>("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name = 'trg_child'").First());
        Assert.Equal(2, db.Query<int>("SELECT COUNT(*) FROM pragma_table_info('TxMigParent')").First());
    }
}
