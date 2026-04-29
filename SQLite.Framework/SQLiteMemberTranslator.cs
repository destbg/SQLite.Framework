namespace SQLite.Framework;

/// <summary>
/// Translates a custom member call into a SQL fragment.
/// </summary>
/// <param name="callerContext">The context of the caller.</param>
/// <returns>An <see cref="Expression"/> representing the member call.</returns>
public delegate Expression SQLiteMemberTranslator(SQLiteCallerContext callerContext);