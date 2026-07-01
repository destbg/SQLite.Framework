namespace SQLite.Framework.Internals.Interfaces;

/// <summary>
/// Marks the internal LINQ chain wrapper (<see cref="Queryable{T}" />) so the translator can tell it
/// apart from mapped tables, CTEs and other queryables without knowing the row type. A stored query
/// that is reused inside another query is one of these and it must be expanded into the expression
/// it holds before translation.
/// </summary>
internal interface IChainQueryable
{
}
