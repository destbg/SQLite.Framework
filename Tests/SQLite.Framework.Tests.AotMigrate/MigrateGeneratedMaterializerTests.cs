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
        db.Table<AotMigrateRow>().Schema.Migrate();

        db.Table<AotMigrateRow>().Add(new AotMigrateRow { Id = 1, Name = "x" });
        Assert.Equal("x", db.Table<AotMigrateRow>().Single().Name);
    }
}
