using System.Linq.Expressions;

namespace SQLite.Framework;

/// <summary>
/// Provides expression translation capabilities for method call interceptors.
/// </summary>
public interface ISQLExpressionVisitor
{
    /// <summary>
    /// Visits and translates an expression into its SQL representation.
    /// </summary>
    Expression Visit(Expression node);

    /// <summary>
    /// The storage options for the current database.
    /// </summary>
    SQLiteStorageOptions StorageOptions { get; }
}
