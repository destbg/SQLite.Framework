using SQLite.Framework.Enums;

namespace SQLite.Framework.Internals.Models;

internal class QueryContext
{
    public required SQLiteDataReader Reader { get; init; }
    public required Dictionary<string, (int Index, SQLiteColumnType ColumnType)> Columns { get; init; }
}