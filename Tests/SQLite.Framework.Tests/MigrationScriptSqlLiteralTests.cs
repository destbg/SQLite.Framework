using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MigrationScriptSqlLiteralTests
{
    [Fact]
    public void ScriptKeepsParameterNameTextInsideStringLiterals()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"ScriptLiteralRows\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT, \"Tag\" TEXT)");

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m.Sql(
                "INSERT INTO \"ScriptLiteralRows\" (\"Id\", \"Body\", \"Tag\") VALUES (1, 'mail to @tag', @tag)",
                new SQLiteParameter { Name = "@tag", Value = "sales" }))
            .Script();

        Assert.Equal(
        [
            "INSERT INTO \"ScriptLiteralRows\" (\"Id\", \"Body\", \"Tag\") VALUES (1, 'mail to @tag', 'sales')",
            "PRAGMA user_version = 1",
        ], statements);
    }
}
