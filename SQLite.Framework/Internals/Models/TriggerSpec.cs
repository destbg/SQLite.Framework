namespace SQLite.Framework.Internals.Models;

/// <summary>
/// A trigger declared on the model. Holds the parts needed to build the
/// <c>CREATE TRIGGER</c> statement so it can be created and reconciled like an index.
/// </summary>
internal sealed class TriggerSpec
{
    private readonly Func<string?> whenFactory;
    private readonly Func<string> bodyFactory;

    public TriggerSpec(string name, SQLiteTriggerTiming timing, SQLiteTriggerEvent @event, Func<string?> whenFactory, Func<string> bodyFactory)
    {
        this.whenFactory = whenFactory;
        this.bodyFactory = bodyFactory;
        Name = name;
        Timing = timing;
        Event = @event;
    }

    public string Name { get; }
    public SQLiteTriggerTiming Timing { get; }
    public SQLiteTriggerEvent Event { get; }
    public string? WhenSql => whenFactory();
    public string BodySql => bodyFactory();
}
