namespace SQLite.Framework;

/// <summary>
/// Collects column values to write while a table is rebuilt during <c>Migrate(...)</c>. Use it to
/// fill a new <c>NOT NULL</c> column that has no default or to recompute a column from the old
/// row. Each value is read from the old row and inserted into the rebuilt table. A column not set
/// here is copied across unchanged when it still exists.
/// </summary>
public sealed class SQLiteMigrationBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
{
    private readonly SQLiteDatabase database;
    private readonly TableMapping mapping;
    private readonly List<MigrationSetValue> sets = [];

    internal SQLiteMigrationBuilder(SQLiteDatabase database, TableMapping mapping)
    {
        this.database = database;
        this.mapping = mapping;
    }

    internal IReadOnlyList<MigrationSetValue> Sets => sets;

    /// <summary>
    /// Sets the target column to a constant value. The target is a property on
    /// <typeparamref name="T" /> or <c>SQLiteColumn.Of&lt;TValue&gt;(row, "Name")</c> for a column
    /// with no CLR property.
    /// </summary>
    public SQLiteMigrationBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, TValue value)
    {
        sets.Add(new MigrationSetValue
        {
            Column = ResolveWritableColumn(column),
            ValueSql = ConverterSql.WrapParameter(SqlLiteralHelper.FormatLiteral(value, database.Options), typeof(TValue), database.Options),
        });
        return this;
    }

    /// <summary>
    /// Sets the target column to an expression evaluated over the old row. The target is a property
    /// on <typeparamref name="T" /> or <c>SQLiteColumn.Of&lt;TValue&gt;(row, "Name")</c>. The value
    /// expression may read any column of the old row, including ones with no CLR property through
    /// <c>SQLiteColumn.Of&lt;TValue&gt;(row, "Name")</c>.
    /// </summary>
    public SQLiteMigrationBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, Expression<Func<T, TValue>> value)
    {
        SetReadColumnCollector reads = new(mapping, value.Parameters[0]);
        reads.Visit(CommonHelpers.Inline(value.Body));
        sets.Add(new MigrationSetValue
        {
            Column = ResolveWritableColumn(column),
            ValueSql = BareSqlTranslator.Translate(database, mapping, value),
            ReadColumns = reads.Columns,
        });
        return this;
    }

    /// <summary>
    /// Re-encodes an existing column through its current converter while the table is rebuilt. Use
    /// this when a column's converter changed the stored form, for example a JSON string column that
    /// switched to <c>JSONB</c> or a <c>JSONB</c> column that switched back to a JSON string, so the
    /// old value is rewritten in the new form instead of copied as is. The rewrite runs inside the
    /// rebuild, which a STRICT table needs because it will not store the old form in the new column.
    /// The re-encode uses the converter's write wrap when it has one, or <c>json(...)</c> for a JSON
    /// text converter. It throws when the converter has neither, since there is no SQL expression to
    /// rewrite the stored value.
    /// </summary>
    public SQLiteMigrationBuilder<T> Reconvert<TValue>(Expression<Func<T, TValue>> column)
    {
        string name = ResolveWritableColumn(column);
        sets.Add(new MigrationSetValue
        {
            Column = name,
            ValueSql = BuildReconvertSql(name, typeof(TValue)),
            ReadColumns = [name],
            RunInRebuild = true,
        });
        return this;
    }

    private string BuildReconvertSql(string columnName, Type valueType)
    {
        string quoted = IdentifierGuard.Quote(columnName);
        Type lookupType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        database.Options.TypeConverters.TryGetValue(lookupType, out ISQLiteTypeConverter? converter);

        if (converter?.ParameterSqlExpression is { } writeWrap)
        {
            return string.Format(writeWrap, quoted);
        }

        if (converter is IJsonTypeInfoSource)
        {
            return $"json({quoted})";
        }

        throw new InvalidOperationException(
            $"Cannot Reconvert column '{columnName}' on table '{mapping.TableName}'. Its converter has no " +
            "write wrap and is not a JSON text converter, so there is no SQL expression that can rewrite the " +
            "stored value while the table is rebuilt. Set the column to a value expression instead.");
    }

    private string ResolveWritableColumn<TValue>(Expression<Func<T, TValue>> column)
    {
        string name = CommonHelpers.Resolve(mapping, column);
        if (mapping.ComputedColumns.Any(c => string.Equals(c.Column.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Column '{name}' on table '{mapping.TableName}' is computed, so a migration fill cannot write it. Remove the Set call and let SQLite compute the value.");
        }

        if (!mapping.Columns.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
            && !mapping.ShadowColumns.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Column '{name}' is not a column on table '{mapping.TableName}'. Set can only fill a mapped column or a shadow column declared in OnModelCreating.");
        }

        return name;
    }
}
