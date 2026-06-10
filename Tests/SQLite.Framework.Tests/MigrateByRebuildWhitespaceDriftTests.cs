using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MigrateByRebuildWhitespaceDriftTests
{
    private static string CanonicalDdl()
    {
        using TestDatabase scratch = new();
        scratch.Schema.CreateTable<MigSimple>();
        return scratch.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'MigSimple'")!;
    }

    [Fact]
    public void MigrateByRebuild_WhitespaceOnlyDrift_DoesNotRebuild()
    {
        string canonical = CanonicalDdl();
        string spaced = canonical.Replace("(", "(  ");

        using TestDatabase db = new();
        db.Execute(spaced);
        db.Table<MigSimple>().Add(new MigSimple { Id = 1, Name = "keep" });

        int statements = db.Schema.MigrateByRebuild<MigSimple>();

        string? live = db.ExecuteScalar<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'MigSimple'");

        Assert.Equal(0, statements);
        Assert.Contains("(  ", live);
        Assert.Equal("keep", db.Table<MigSimple>().Single().Name);
    }

    [Fact]
    public void MigrateByRebuild_RealDrift_StillRebuilds()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"MigSimple\" (\"Id\" INTEGER NOT NULL PRIMARY KEY)");

        db.Schema.MigrateByRebuild<MigSimple>();

        string? live = db.ExecuteScalar<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'MigSimple'");
        Assert.Contains("Name", live);
    }
}
