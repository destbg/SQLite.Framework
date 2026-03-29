using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Models;

namespace SQLite.Framework;

/// <summary>
/// Represents a Common Table Expression (CTE) that can be used in a query.
/// </summary>
public abstract class SQLiteCte : BaseSQLiteTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteCte"/> class.
    /// </summary>
    protected SQLiteCte(SQLiteDatabase database, LambdaExpression query) : base(database)
    {
        Query = query;
    }

    /// <summary>
    /// The lambda expression that defines the CTE body.
    /// </summary>
    public LambdaExpression Query { get; }

    /// <inheritdoc />
    public override Type ElementType => Query.ReturnType.GetGenericArguments()[0];

    /// <inheritdoc />
    public override Expression Expression => Expression.Constant(this);

    /// <inheritdoc />
    public override IQueryProvider Provider => Database;

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
    {
        return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
    }
}

/// <summary>
/// Represents a typed Common Table Expression (CTE) that can be used in a query.
/// </summary>
public class SQLiteCte<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T> : SQLiteCte, IQueryable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteCte{T}"/> class.
    /// </summary>
    public SQLiteCte(SQLiteDatabase database, Expression<Func<IQueryable<T>>> expression) : base(database, expression)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteCte{T}"/> class.
    /// </summary>
    public SQLiteCte(SQLiteDatabase database, Expression<Func<IQueryable<T>, IQueryable<T>>> expression) : base(database, expression)
    {
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
    }

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }
}
