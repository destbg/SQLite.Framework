using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MigrationScriptSavepointStatementTests
{
    [Fact]
    public void ScriptKeepsAUserStatementThatStartsWithASavepoint()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"CfgRows\" (\"Id\" INTEGER PRIMARY KEY, \"X\" INTEGER)");
        db.Execute("INSERT INTO \"CfgRows\" (\"Id\", \"X\") VALUES (1, 0)");

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m.Sql("SAVEPOINT fix; UPDATE \"CfgRows\" SET \"X\" = 1; RELEASE fix"))
            .Script();

        Assert.Equal(
        [
            "SAVEPOINT fix; UPDATE \"CfgRows\" SET \"X\" = 1; RELEASE fix",
            "PRAGMA user_version = 1",
        ], statements);
    }
}
