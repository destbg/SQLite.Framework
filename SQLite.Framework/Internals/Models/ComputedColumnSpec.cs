namespace SQLite.Framework.Internals.Models;

internal sealed class ComputedColumnSpec
{
    public ComputedColumnSpec(TableColumn column, string expressionSql, bool stored)
    {
        Column = column;
        ExpressionSql = expressionSql;
        Stored = stored;
    }

    public TableColumn Column { get; }
    public string ExpressionSql { get; }
    public bool Stored { get; }
}
