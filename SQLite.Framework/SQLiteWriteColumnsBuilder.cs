namespace SQLite.Framework;

/// <summary>
/// Collects extra column values to write alongside an <c>Add</c> or <c>Update</c>. Reach it through
/// <see cref="SQLiteTable{T}.WithColumns" />. Use it to fill a column that has no CLR property, such
/// as a shadow column declared with <see cref="SQLiteEntityTypeBuilder{T}.Column" />, or to override
/// a mapped column with a database expression.
/// </summary>
public sealed class SQLiteWriteColumnsBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
{
    private readonly SQLiteDatabase database;
    private readonly TableMapping mapping;
    private readonly List<(string Column, string ValueSql)> columns = [];

    internal SQLiteWriteColumnsBuilder(SQLiteDatabase database, TableMapping mapping)
    {
        this.database = database;
        this.mapping = mapping;
    }

    internal IReadOnlyList<(string Column, string ValueSql)> Columns => columns;

    /// <summary>
    /// True when any value expression reads a column of the row. Such a value cannot run on an
    /// <c>Add</c> (or the inserted side of an <c>Upsert</c>), where the row does not exist yet.
    /// </summary>
    internal bool ReferencesRow { get; private set; }

    /// <summary>
    /// Sets the target column to a constant value. The target is a property on
    /// <typeparamref name="T" /> or <c>SQLiteColumn.Of&lt;TValue&gt;(row, "Name")</c> for a column
    /// with no CLR property.
    /// </summary>
    public SQLiteWriteColumnsBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, TValue value)
    {
        columns.Add((ColumnTargetResolver.Resolve(mapping, column), SqlLiteralHelper.FormatLiteral(value)));
        return this;
    }

    /// <summary>
    /// Sets the target column to an expression translated to SQL. On an <c>Update</c> the expression
    /// may read the row's other columns. On an <c>Add</c> use a constant or a function such as
    /// <c>_ =&gt; SQLiteFunctions.UnixEpoch()</c>, because SQLite cannot read another column of the
    /// row being inserted.
    /// </summary>
    public SQLiteWriteColumnsBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, Expression<Func<T, TValue>> value)
    {
        if (ParameterUsageFinder.Uses(value))
        {
            ReferencesRow = true;
        }

        columns.Add((ColumnTargetResolver.Resolve(mapping, column), BareSqlTranslator.Translate(database, mapping, value)));
        return this;
    }
}
