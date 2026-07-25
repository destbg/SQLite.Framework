using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MigrationScriptRealLiteralTests
{
    [Fact]
    public void ScriptKeepsTheRealStorageClassForWholeNumberDoubles()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"RawVals\" (\"V\")");

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m.Sql(
                "INSERT INTO \"RawVals\" (\"V\") VALUES (@v)",
                new SQLiteParameter { Name = "@v", Value = 3.0 }))
            .Script();

        Assert.Equal(
        [
            "INSERT INTO \"RawVals\" (\"V\") VALUES (3.0)",
            "PRAGMA user_version = 1",
        ], statements);
    }
}
