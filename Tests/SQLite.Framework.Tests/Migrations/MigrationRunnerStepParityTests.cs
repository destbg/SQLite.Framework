using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MrFreshDrop")]
public sealed class MrFreshDropRow
{
    [Key]
    public int Id { get; set; }

    public int Keep { get; set; }
}

[Table("MrRename")]
public sealed class MrRenameRow
{
    [Key]
    public int Id { get; set; }

    public string NewName { get; set; } = "";
}

[Table("MrMapDrop")]
public sealed class MrMapDropRow
{
    [Key]
    public int Id { get; set; }

    public int Keep { get; set; }
}

[Table("MrShadow")]
public sealed class MrShadowRow
{
    [Key]
    public int Id { get; set; }

    public int Keep { get; set; }
}

public class MigrationRunnerStepParityTests
{
    [Fact]
    public void FreshDatabase_RawSqlCreatesTableThenDropTable_TableIsDropped()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.Sql("CREATE TABLE \"MrStage\" (\"Id\" INTEGER PRIMARY KEY)"))
            .Version(2, m => m.DropTable("MrStage"))
            .Migrate();

        Assert.False(db.Schema.TableExists("MrStage"));
    }

    [Fact]
    public void FreshDatabase_RawSqlCreatesColumnThenDropColumn_ColumnIsDropped()
    {
        using TestDatabase db = new();

        db.Schema.Migrations()
            .Version(1, m => m.Sql("CREATE TABLE \"MrFreshDrop\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" INTEGER NOT NULL, \"Gone\" INTEGER)"))
            .Version(2, m => m.DropColumn<MrFreshDropRow>("Gone"))
            .Migrate();

        Assert.DoesNotContain(db.Schema.ListColumns("MrFreshDrop"), c => c.Name == "Gone");
    }

    [Fact]
    public void Migrate_RenameColumnSourceAbsentAfterDirectCreate_DoesNotAbort()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.CreateTable<MrRenameRow>();
        db.Execute("INSERT INTO \"MrRename\" (\"Id\", \"NewName\") VALUES (1, 'keep')");

        db.Schema.Migrations()
            .Version(1, m => m.RenameColumn<MrRenameRow>("OldName", "NewName").TableChanged<MrRenameRow>())
            .Migrate();

        Assert.Equal(1, db.Pragmas.UserVersion);
        Assert.Equal("keep", db.Table<MrRenameRow>().Single().NewName);
    }

    [Fact]
    public void Migrate_DropColumnStillMappedByModel_LeavesTableReadable()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"MrMapDrop\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"MrMapDrop\" (\"Id\", \"Keep\") VALUES (1, 5)");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<MrMapDropRow>().DropColumn<MrMapDropRow>("Keep"))
            .Migrate();

        Exception? ex = Record.Exception(() => db.Table<MrMapDropRow>().ToList());
        Assert.Null(ex);
    }

    [Fact]
    public void Migrate_DropColumnThatIsAModelShadowColumn_KeepsColumn()
    {
        using ModelTestDatabase db = new(model => model.Entity<MrShadowRow>()
            .Column("Tag", SQLiteColumnType.Integer, nullable: true));
        db.Execute("CREATE TABLE \"MrShadow\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" INTEGER NOT NULL, \"Tag\" INTEGER)");
        db.Execute("INSERT INTO \"MrShadow\" (\"Id\", \"Keep\", \"Tag\") VALUES (1, 5, 9)");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<MrShadowRow>("Tag"))
            .Migrate();

        Assert.True(db.Schema.Table<MrShadowRow>().ColumnExists("Tag"));
        Assert.Equal(9, db.ExecuteScalar<long>("SELECT \"Tag\" FROM \"MrShadow\" WHERE \"Id\" = 1"));
    }
}
