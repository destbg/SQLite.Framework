namespace SQLite.Framework;

/// <summary>
/// A queryable backed by a SQLite pragma table-valued function (such as
/// <c>pragma_table_info(name)</c>). Created by the helper methods on
/// <see cref="SQLiteSchema" /> and translated to a direct pragma call in SQL. Supports the
/// full LINQ surface (<c>Where</c>, <c>Select</c>, <c>Join</c> and so on).
/// </summary>
public sealed class SQLitePragmaTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T> : BaseSQLiteQueryable, IPragmaTableSource, IQueryable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLitePragmaTable{T}"/> class.
    /// </summary>
    /// <param name="database">The database the pragma runs against.</param>
    /// <param name="pragmaName">The pragma function name, for example <c>pragma_table_info</c>.</param>
    /// <param name="arguments">The arguments passed to the pragma. Most pragmas take a single string argument.</param>
    public SQLitePragmaTable(SQLiteDatabase database, string pragmaName, params object?[] arguments)
        : base(database)
    {
        PragmaName = pragmaName;
        Arguments = arguments;
    }

    /// <summary>
    /// The pragma function name, for example <c>pragma_table_info</c>.
    /// </summary>
    public string PragmaName { get; }

    /// <summary>
    /// The arguments passed to the pragma.
    /// </summary>
    public IReadOnlyList<object?> Arguments { get; }

    /// <inheritdoc />
    public override Type ElementType => typeof(T);

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
