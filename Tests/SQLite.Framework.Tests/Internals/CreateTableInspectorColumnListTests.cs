using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Tests;

public class CreateTableInspectorColumnListTests
{
    [Fact]
    public void WithoutRowidInsideColumnListIsNotTheTableClause()
    {
        string createSql = "CREATE TABLE \"inspector_wor_items\" (\"Id\" INTEGER PRIMARY KEY, b WITHOUT ROWID)";

        Assert.False(CreateTableInspector.HasWithoutRowIdClause(createSql));
    }
}
