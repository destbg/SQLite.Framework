using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MigrationScriptParameterPrefixTests
{
    [Fact]
    public void MigrateBindsAPlaceholderWhosePrefixDiffersFromTheParameterName()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SecHScriptPrefix\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER)");
        db.Execute("INSERT INTO \"SecHScriptPrefix\" (\"Id\", \"Value\") VALUES (1, 0)");

        db.Schema.Migrations()
            .Version(1, m => m.Sql(
                "UPDATE \"SecHScriptPrefix\" SET \"Value\" = :v WHERE \"Id\" = 1",
                new SQLiteParameter { Name = "@v", Value = 5 }))
            .Migrate();

        Assert.Equal(5, db.ExecuteScalar<int>("SELECT \"Value\" FROM \"SecHScriptPrefix\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void ScriptInlinesAPlaceholderWhosePrefixDiffersFromTheParameterName()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SecHScriptPrefix\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER)");
        db.Execute("INSERT INTO \"SecHScriptPrefix\" (\"Id\", \"Value\") VALUES (1, 0)");

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m.Sql(
                "UPDATE \"SecHScriptPrefix\" SET \"Value\" = :v WHERE \"Id\" = 1",
                new SQLiteParameter { Name = "@v", Value = 5 }))
            .Script();

        Assert.Equal(
        [
            "UPDATE \"SecHScriptPrefix\" SET \"Value\" = 5 WHERE \"Id\" = 1",
            "PRAGMA user_version = 1",
        ], statements);
    }

    [Fact]
    public void ScriptInlinesPrefixlessParameterNames()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SecHScriptBare\" (\"Id\" INTEGER PRIMARY KEY, \"A\" INTEGER, \"B\" INTEGER)");

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m.Sql(
                "INSERT INTO \"SecHScriptBare\" (\"Id\", \"A\", \"B\") VALUES (1, @v, @v2)",
                new SQLiteParameter { Name = "v", Value = 1 },
                new SQLiteParameter { Name = "v2", Value = 2 }))
            .Script();

        Assert.Equal(
        [
            "INSERT INTO \"SecHScriptBare\" (\"Id\", \"A\", \"B\") VALUES (1, 1, 2)",
            "PRAGMA user_version = 1",
        ], statements);
    }

    [Fact]
    public void ScriptInlinesAPositionalParameter()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SecHScriptPositional\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER)");
        db.Execute("INSERT INTO \"SecHScriptPositional\" (\"Id\", \"Value\") VALUES (1, 0)");

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m.Sql(
                "UPDATE \"SecHScriptPositional\" SET \"Value\" = ?1 WHERE \"Id\" = 1",
                new SQLiteParameter { Name = "?1", Value = 5 }))
            .Script();

        Assert.Contains(
            "UPDATE \"SecHScriptPositional\" SET \"Value\" = 5 WHERE \"Id\" = 1",
            statements);
    }
}
