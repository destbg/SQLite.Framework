namespace SQLite.Framework.Internals.Models;

/// <summary>
/// A trigger declared on the model. Holds the parts needed to build the
/// <c>CREATE TRIGGER</c> statement so it can be created and reconciled like an index.
/// </summary>
internal sealed class TriggerSpec
{
    public TriggerSpec(string name, SQLiteTriggerTiming timing, SQLiteTriggerEvent @event, string? whenSql, string bodySql)
    {
        Name = name;
        Timing = timing;
        Event = @event;
        WhenSql = whenSql;
        BodySql = bodySql;
    }

    public string Name { get; }
    public SQLiteTriggerTiming Timing { get; }
    public SQLiteTriggerEvent Event { get; }
    public string? WhenSql { get; }
    public string BodySql { get; }
}
