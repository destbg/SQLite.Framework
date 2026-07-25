using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SchemaListingNamePatternTests
{
    [Fact]
    public void ListTablesIncludesAUserTableNamedLikeSqlitePrefix()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"sqliteH21lCache\" (\"Id\" INTEGER)");

        IReadOnlyList<string> tables = db.Schema.ListTables();

        Assert.Contains("sqliteH21lCache", tables);
    }

    [Fact]
    public void ListViewsIncludesAUserViewNamedLikeSqlitePrefix()
    {
        using TestDatabase db = new();
        db.Execute("CREATE VIEW \"sqliteH21lView\" AS SELECT 1 AS \"Id\"");

        IReadOnlyList<string> views = db.Schema.ListViews();

        Assert.Contains("sqliteH21lView", views);
    }

    [Fact]
    public void ListIndexesIncludesAUserIndexNamedLikeSqlitePrefix()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H21lPatternRows\" (\"Id\" INTEGER)");
        db.Execute("CREATE INDEX \"sqliteH21lIdx\" ON \"H21lPatternRows\"(\"Id\")");

        IReadOnlyList<string> indexes = db.Schema.ListIndexes();

        Assert.Contains("sqliteH21lIdx", indexes);
    }

    [Fact]
    public void ListIndexesForOneTableIncludesAUserIndexNamedLikeSqlitePrefix()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H21lPatternRows2\" (\"Id\" INTEGER)");
        db.Execute("CREATE INDEX \"sqliteH21lIdx2\" ON \"H21lPatternRows2\"(\"Id\")");

        IReadOnlyList<string> indexes = db.Schema.ListIndexes("H21lPatternRows2");

        Assert.Contains("sqliteH21lIdx2", indexes);
    }

    [Fact]
    public void ListTablesExcludesRealSystemTables()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H21lPatternRows3\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT)");
        db.Execute("INSERT INTO \"H21lPatternRows3\" DEFAULT VALUES");

        IReadOnlyList<string> tables = db.Schema.ListTables();

        Assert.DoesNotContain("sqlite_sequence", tables);
        Assert.Contains("H21lPatternRows3", tables);
    }
}
