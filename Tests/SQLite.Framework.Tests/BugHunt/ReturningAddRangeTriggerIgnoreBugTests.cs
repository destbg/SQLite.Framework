using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal sealed class ReturningAutoRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class ReturningAddRangeTriggerIgnoreBugTests
{
    [Fact]
    public void ReturningAddRange_BeforeInsertTriggerIgnore_SkipsRowAndDoesNotThrow()
    {
        using TestDatabase db = new();
        db.Table<ReturningAutoRow>().Schema.CreateTable();
        db.Execute("""
            CREATE TRIGGER trg_bughunt_returning_ignore BEFORE INSERT ON ReturningAutoRow
            FOR EACH ROW
            WHEN NEW.Name = 'skip'
            BEGIN
                SELECT RAISE(IGNORE);
            END;
            """);

        ReturningAutoRow[] items =
        [
            new ReturningAutoRow { Name = "a" },
            new ReturningAutoRow { Name = "skip" },
            new ReturningAutoRow { Name = "b" },
        ];

        List<ReturningAutoRow> rows = db.Table<ReturningAutoRow>().Returning().AddRange(items);

        Assert.Equal(new[] { "a", "b" }, rows.Select(r => r.Name).ToArray());
        Assert.Equal(new[] { "a", "b" }, db.Table<ReturningAutoRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList());
    }
}
