namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Support class only for the LINQ provider.
/// </summary>
internal class Queryable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T> : BaseSQLiteQueryable, IOrderedQueryable<T>, IChainQueryable
{
    public Queryable(SQLiteDatabase database, Expression expression)
        : base(database)
    {
        Expression = expression;
    }

    public override Type ElementType => typeof(T);

    public override Expression Expression { get; }

    public override IQueryProvider Provider => Database;

    public override IEnumerator<T> GetEnumerator()
    {
        return Database.ExecuteSequenceQuery<T>(Expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
