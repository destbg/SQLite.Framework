using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Models;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Support class only for the LINQ provider.
/// </summary>
internal class Queryable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T> : BaseSQLiteTable, IOrderedQueryable<T>
{
    public Queryable(SQLiteDatabase database, Expression expression)
        : base(database)
    {
        Expression = expression;
    }

    public override IEnumerator<T> GetEnumerator()
    {
        return Database.ExecuteSequenceQuery<T>(Expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override Type ElementType => typeof(T);
    public override Expression Expression { get; }
    public override IQueryProvider Provider => Database;
}