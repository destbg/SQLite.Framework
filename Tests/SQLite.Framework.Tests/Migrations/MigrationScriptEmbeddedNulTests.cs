using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("EmbeddedNulNotes")]
public class EmbeddedNulNoteRow
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

public class MigrationScriptEmbeddedNulTests
{
    [Fact]
    public void ScriptedInsertOfAStringWithAnEmbeddedNulReplaysTheSameBytes()
    {
        EmbeddedNulNoteRow row = new() { Id = 1, Body = "before\0after" };

        using TestDatabase real = new();
        SQLiteMigrationRunner runner = real.Schema.Migrations()
            .Version(1, m => m.CreateTable<EmbeddedNulNoteRow>().Insert(row));
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

    private static List<string> Dump(TestDatabase db)
    {
        return db.Query<string>(
            "SELECT typeof(\"Body\") || '|' || hex(CAST(\"Body\" AS BLOB)) FROM \"EmbeddedNulNotes\" ORDER BY \"Id\"");
    }
}
