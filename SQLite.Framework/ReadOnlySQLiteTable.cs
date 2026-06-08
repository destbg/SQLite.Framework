namespace SQLite.Framework;

/// <summary>
/// A read-only SQLite table. Used for built-in system tables such as <c>sqlite_master</c>
/// and <c>sqlite_sequence</c>, but you can also use it for any entity you want to expose
/// as queryable without the mutation surface of <see cref="SQLiteTable{T}" />. Supports
/// the same LINQ surface (<c>Select</c>, <c>Where</c>, <c>Join</c>, <c>OrderBy</c>, and
/// so on) as a normal table.
/// </summary>
public class ReadOnlySQLiteTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T> : BaseSQLiteTable, IQueryable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlySQLiteTable{T}"/> class.
    /// </summary>
    public ReadOnlySQLiteTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    /// <inheritdoc />
    public override Type ElementType => Table.Type;

    /// <inheritdoc />
    public override Expression Expression => Expression.Constant(this);

    /// <inheritdoc />
    public override IQueryProvider Provider => Database;

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return Database.ExecuteSequenceQuery<T>(Expression).GetEnumerator();
    }
}
