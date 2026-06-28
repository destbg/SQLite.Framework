using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Generated;

namespace SQLite.Framework.Tests.AotMigrate;

public sealed class AotMigrateRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public sealed class AotAddMigration : ISQLiteMigration
{
    public static int Version => 1;

    public void Apply(SQLiteMigrationStep step)
    {
        step.TableChanged<AotMigrateRow>();
    }
}

public class MigrateGeneratedMaterializerTests
{
    private static SQLiteDatabase CreateDatabase()
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        builder.UseGeneratedMaterializers();
        builder.DisableReflectionFallback();
        return new SQLiteDatabase(builder.Build());
    }

    [Fact]
    public void Migrate_WithGeneratedMaterializers_DoesNotFallBackToReflection()
    {
        using SQLiteDatabase db = CreateDatabase();

        db.Table<AotMigrateRow>().Schema.CreateTable();
        db.Schema.Migrations().Version(1, m => m.TableChanged<AotMigrateRow>()).Migrate();

        db.Table<AotMigrateRow>().Add(new AotMigrateRow { Id = 1, Name = "x" });
        Assert.Equal("x", db.Table<AotMigrateRow>().Single().Name);
    }

    [Fact]
    public void Add_WithGeneratedMaterializers_DoesNotFallBackToReflection()
    {
        using SQLiteDatabase db = CreateDatabase();

        db.Schema.Migrations().Add<AotAddMigration>().Migrate();

        db.Table<AotMigrateRow>().Add(new AotMigrateRow { Id = 1, Name = "x" });
        Assert.Equal("x", db.Table<AotMigrateRow>().Single().Name);
    }
}
