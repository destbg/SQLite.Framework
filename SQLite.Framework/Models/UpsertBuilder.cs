using System.Linq.Expressions;
using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Models;

/// <summary>
/// Entry point for building an <c>INSERT INTO ... ON CONFLICT (...) DO ...</c> statement.
/// Returned by <c>SQLiteTable&lt;T&gt;.Upsert</c> and <c>UpsertRange</c>.
/// </summary>
public sealed class UpsertBuilder<T>
{
    private UpsertConflictTarget<T>? target;

    internal UpsertBuilder()
    {
    }

    /// <summary>
    /// Picks the conflict target. Pass a single property reference for one column or an
    /// anonymous type for a composite. For example:
    /// <c>OnConflict(b =&gt; b.Id)</c> or <c>OnConflict(b =&gt; new { b.AuthorId, b.Title })</c>.
    /// </summary>
    public UpsertConflictTarget<T> OnConflict<TKey>(Expression<Func<T, TKey>> conflictTarget)
    {
        if (target != null)
        {
            throw new InvalidOperationException("OnConflict was already called for this Upsert.");
        }

        IReadOnlyList<string> columnPaths = UpsertExpressionParser.ResolveConflictColumns(conflictTarget);
        target = new UpsertConflictTarget<T>(columnPaths);
        return target;
    }

    internal UpsertConflictTarget<T> Build()
    {
        if (target == null)
        {
            throw new InvalidOperationException("Upsert configuration is missing an OnConflict(...) call.");
        }

        return target;
    }
}
