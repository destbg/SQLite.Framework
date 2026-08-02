using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecDScriptBoundaryRows")]
public class SecDScriptBoundaryRow
{
    [Key]
    public int Id { get; set; }

    public string? Body { get; set; }
}

public class SecDScriptParameterBoundaryTests
{
    [Fact]
    public void ScriptedStatementReplaysTheValueTheRunBound()
    {
        using TestDatabase real = new();
        SQLiteMigrationRunner runner = real.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SecDScriptBoundaryRow>()
                .Sql(
                    "INSERT INTO \"SecDScriptBoundaryRows\" (\"Id\", \"Body\") VALUES (@id, @id2)",
                    new SQLiteParameter { Name = "@id", Value = 7 }));
        IReadOnlyList<string> statements = runner.Script();
        runner.Migrate();

        using TestDatabase replay = new();
        foreach (string statement in statements)
        {
            if (!statement.StartsWith("--"))
            {
                replay.Execute(statement);
            }
        }

        Assert.Equal(Dump(real), Dump(replay));
    }

    private static List<string?> Dump(TestDatabase db)
    {
        return db.Query<string?>(
            "SELECT \"Id\" || '|' || IFNULL(\"Body\", '<null>') FROM \"SecDScriptBoundaryRows\" ORDER BY \"Id\"");
    }
}
