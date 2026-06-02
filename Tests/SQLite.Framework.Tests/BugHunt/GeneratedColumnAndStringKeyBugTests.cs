using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class GeneratedColumnAndStringKeyBugTests
{
    [Fact]
    public void StringKey_Ddl_HasNotNull()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<StrKeyProbe>();

        string? ddl = db.ExecuteScalar<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'StrKeyProbe'");

        Assert.NotNull(ddl);
        Assert.Contains("NOT NULL", ddl);
    }

    [Fact]
    public void StringKey_RejectsNullKeyInsert()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<StrKeyProbe>();

        Assert.ThrowsAny<Exception>(() =>
            db.Execute("INSERT INTO StrKeyProbe (\"Tag\") VALUES (NULL)"));
    }
}

public class StrKeyProbe
{
    [Key]
    public required string Tag { get; set; }
}
