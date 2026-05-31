namespace SQLite.Framework.Internals.Models;

/// <summary>
/// A trigger declared on the model. Holds the parts needed to build the
/// <c>CREATE TRIGGER</c> statement so it can be created and reconciled like an index.
/// </summary>
internal sealed class TriggerSpec
{
    public TriggerSpec(string name, SQLiteTriggerTiming timing, SQLiteTriggerEvent @event, bool forEachRow, string? whenSql, string bodySql)
    {
        Name = name;
        Timing = timing;
        Event = @event;
        ForEachRow = forEachRow;
        WhenSql = whenSql;
        BodySql = bodySql;
    }

    public string Name { get; }
    public SQLiteTriggerTiming Timing { get; }
    public SQLiteTriggerEvent Event { get; }
    public bool ForEachRow { get; }
    public string? WhenSql { get; }
    public string BodySql { get; }
}
