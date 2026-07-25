using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("HookIgnoreAutoRow")]
public class HookIgnoreAutoRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ReturningColumnHookTriggerIgnoreAutoIncrementParityTests
{
    [Fact]
    public void Returning_Add_WithColumnHook_WhenTriggerIgnoresInsert_LeavesKeyUnset()
    {
        using TestDatabase db = new(b => b.OnAdd<HookIgnoreAutoRow>((_, _, _) => true));
        db.Table<HookIgnoreAutoRow>().Schema.CreateTable();
        db.Execute("""
            CREATE TRIGGER trg_hook_ignore BEFORE INSERT ON HookIgnoreAutoRow
            FOR EACH ROW
            WHEN NEW.Name = 'skip'
            BEGIN
                SELECT RAISE(IGNORE);
            END;
            """);

        db.Table<HookIgnoreAutoRow>().Returning().Add(new HookIgnoreAutoRow { Name = "first" });

        HookIgnoreAutoRow ignored = new() { Name = "skip" };
        db.Table<HookIgnoreAutoRow>().Returning().Add(ignored);

        Assert.Equal(0, ignored.Id);
        Assert.Equal(1, db.Table<HookIgnoreAutoRow>().Count());
    }
}
