using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SecDScriptEmptyParameterNameTests
{
    [Fact(Timeout = 120000)]
    public void ScriptSkipsAParameterWithAnEmptyName()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SecDEmptyNameRows\" (\"Id\" INTEGER PRIMARY KEY)");
        TestContext.Current.CancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<string> statements = db.Schema.Migrations()
            .Version(1, m => m.Sql(
                "INSERT INTO \"SecDEmptyNameRows\" (\"Id\") VALUES (1)",
                new SQLiteParameter { Name = "", Value = 7 }))
            .Script();

        Assert.Equal(
        [
            "INSERT INTO \"SecDEmptyNameRows\" (\"Id\") VALUES (1)",
            "PRAGMA user_version = 1",
        ], statements);
    }
}
