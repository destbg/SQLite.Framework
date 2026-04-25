using SQLite.Framework.Enums;

namespace SQLite.Framework;

/// <summary>
/// A cross-cutting hook that runs before every CRUD action in the framework. Receives the
/// entity (untyped) and the action the framework was about to perform, and returns the action
/// to actually perform. The hook may also mutate the entity.
/// </summary>
/// <param name="database">The database the action runs against.</param>
/// <param name="entity">The entity the action was called for.</param>
/// <param name="action">The action the framework was about to perform.</param>
/// <returns>
/// The action to actually run. Return the same value to proceed unchanged. Return
/// <see cref="SQLiteAction.Skip" /> to do nothing for this row. Return a different action
/// (for example <see cref="SQLiteAction.Update" /> instead of <see cref="SQLiteAction.Remove" />)
/// to reroute the operation.
/// </returns>
public delegate SQLiteAction SQLiteActionHook(SQLiteDatabase database, object entity, SQLiteAction action);
