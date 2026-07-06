using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("JsonNulNotes")]
public class JsonNulNoteRow
{
    [Key]
    public int Id { get; set; }

    public List<string> Tags { get; set; } = [];
}

public class MigrationScriptJsonNulTests
{
    [Fact]
    public void ScriptedInsertOfAJsonListWithAnEmbeddedNulReplaysTheSameBytes()
    {
        JsonNulNoteRow row = new() { Id = 1, Tags = ["before\0after"] };

        using TestDatabase real = new(JsonOptions);
        SQLiteMigrationRunner runner = real.Schema.Migrations()
            .Version(1, m => m.CreateTable<JsonNulNoteRow>().Insert(row));
        IReadOnlyList<string> statements = runner.Script();
        runner.Migrate();

        using TestDatabase replay = new(JsonOptions);
        foreach (string statement in statements)
        {
            if (!statement.StartsWith("--"))
            {
                replay.Execute(statement);
            }
        }

        Assert.Equal(Dump(real), Dump(replay));
    }

    private static void JsonOptions(SQLiteOptionsBuilder builder)
    {
        builder.TypeConverters[typeof(List<string>)] = new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString);
    }

    private static List<string> Dump(TestDatabase db)
    {
        return db.Query<string>(
            "SELECT typeof(\"Tags\") || '|' || hex(CAST(\"Tags\" AS BLOB)) FROM \"JsonNulNotes\" ORDER BY \"Id\"");
    }
}
