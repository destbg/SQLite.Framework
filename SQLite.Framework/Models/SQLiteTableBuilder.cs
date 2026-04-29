using System.Globalization;
using System.Text;

namespace SQLite.Framework.Models;

/// <summary>
/// Fluent builder for creating a table and its associated indexes, CHECK constraints, and
/// computed columns at the same time. Reach an instance with
/// <see cref="SQLiteSchema.Table{T}" />.
/// </summary>
public sealed class SQLiteTableBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
{
    private readonly SQLiteDatabase database;
    private readonly TableMapping mapping;
    private readonly List<ComputedColumnSpec> computed = [];
    private readonly List<CheckConstraintSpec> checks = [];
    private readonly List<IndexSpec> indexes = [];

    internal SQLiteTableBuilder(SQLiteSchema schema)
    {
        database = schema.Database;
        mapping = database.TableMapping<T>();
    }

    /// <summary>
    /// Adds a generated (computed) column. The column is computed from <paramref name="sql" /> on
    /// every read when <paramref name="stored" /> is <see langword="false" /> (the default), or
    /// stored on disk when <paramref name="stored" /> is <see langword="true" />.
    /// </summary>
    /// <param name="column">The property on the entity that maps to the computed column.</param>
    /// <param name="sql">Expression that produces the column value, translated to SQL.</param>
    /// <param name="stored">When <see langword="true" />, the column is stored on disk. Default is virtual.</param>
    public SQLiteTableBuilder<T> Computed<TValue>(Expression<Func<T, TValue>> column, Expression<Func<T, TValue>> sql, bool stored = false)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(sql);

        TableColumn target = ResolveTargetColumn(column);
        string expressionSql = TranslateBareSql(sql);

        computed.Add(new ComputedColumnSpec(target, expressionSql, stored));
        return this;
    }

    /// <summary>
    /// Adds a table-level CHECK constraint. <paramref name="predicate" /> is translated to SQL the
    /// same way <c>Where</c> clauses are.
    /// </summary>
    /// <param name="predicate">The condition every row must satisfy.</param>
    /// <param name="name">Optional constraint name. When set, emits
    /// <c>CONSTRAINT &lt;name&gt; CHECK (...)</c>; otherwise emits a bare <c>CHECK (...)</c>.</param>
    public SQLiteTableBuilder<T> Check(Expression<Func<T, bool>> predicate, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        string sql = TranslateBareSql(predicate);
        checks.Add(new CheckConstraintSpec(sql, name));
        return this;
    }

    /// <summary>
    /// Adds an index. Optionally limit the index to rows matching <paramref name="filter" /> for a
    /// partial index.
    /// </summary>
    /// <param name="column">Column to index.</param>
    /// <param name="name">Optional index name. The default is <c>idx_{TableName}_{ColumnName}</c>.</param>
    /// <param name="unique">Whether the index is unique.</param>
    /// <param name="filter">Optional predicate that produces a partial index (<c>WHERE</c> clause).</param>
    public SQLiteTableBuilder<T> Index<TKey>(Expression<Func<T, TKey>> column, string? name = null, bool unique = false, Expression<Func<T, bool>>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(column);

        TableColumn target = ResolveTargetColumn(column);
        string indexName = name ?? $"idx_{mapping.TableName}_{target.Name}";
        string? filterSql = filter == null ? null : TranslateBareSql(filter);

        indexes.Add(new IndexSpec(target.Name, indexName, unique, filterSql));
        return this;
    }

    /// <summary>
    /// Emits the <c>CREATE TABLE IF NOT EXISTS</c> statement plus any indexes recorded on the
    /// builder. Returns the total number of statements run.
    /// </summary>
    public int CreateTable()
    {
        if (mapping.IsFullTextSearch)
        {
            throw new InvalidOperationException("FTS5 tables cannot be created through the fluent builder. Use.Table<T>().Schema.CreateTable() instead.");
        }

        StringBuilder sb = new();
        sb.Append("CREATE TABLE IF NOT EXISTS \"");
        sb.Append(mapping.TableName);
        sb.Append("\" (");

        bool first = true;
        foreach (TableColumn col in mapping.Columns)
        {
            if (!first) sb.Append(", ");
            first = false;

            ComputedColumnSpec? cc = computed.FirstOrDefault(c => c.Column.Name == col.Name);
            if (cc != null)
            {
                sb.Append(col.Name);
                sb.Append(' ');
                sb.Append(col.ColumnType.ToString().ToUpperInvariant());
                sb.Append(" GENERATED ALWAYS AS (");
                sb.Append(cc.ExpressionSql);
                sb.Append(") ");
                sb.Append(cc.Stored ? "STORED" : "VIRTUAL");
            }
            else
            {
                sb.Append(col.GetCreateColumnSql());
            }
        }

        foreach (CheckConstraintSpec check in checks)
        {
            sb.Append(", ");
            if (!string.IsNullOrEmpty(check.Name))
            {
                sb.Append("CONSTRAINT \"");
                sb.Append(check.Name.Replace("\"", "\"\""));
                sb.Append("\" ");
            }
            sb.Append("CHECK (");
            sb.Append(check.Sql);
            sb.Append(')');
        }

        sb.Append(')');

        if (mapping.WithoutRowId)
        {
            sb.Append(" WITHOUT ROWID");
        }

        int count = database.CreateCommand(sb.ToString(), []).ExecuteNonQuery();

        foreach (TableColumn tableColumn in mapping.Columns)
        {
            foreach (IndexedAttribute index in tableColumn.Indices)
            {
                string indexName = index.Name ?? ("idx_" + tableColumn.Name + "_" + index.Order);
                string uniqueClause = index.IsUnique ? "UNIQUE " : string.Empty;
                string sql = $"CREATE {uniqueClause}INDEX IF NOT EXISTS \"{indexName}\" ON \"{mapping.TableName}\" ({tableColumn.Name})";
                count += database.CreateCommand(sql, []).ExecuteNonQuery();
            }
        }

        foreach (IndexSpec index in indexes)
        {
            string uniqueClause = index.Unique ? "UNIQUE " : string.Empty;
            string where = index.FilterSql == null ? string.Empty : $" WHERE {index.FilterSql}";
            string sql = $"CREATE {uniqueClause}INDEX IF NOT EXISTS \"{index.Name}\" ON \"{mapping.TableName}\" ({index.Column}){where}";
            count += database.CreateCommand(sql, []).ExecuteNonQuery();
        }

        return count;
    }

    private TableColumn ResolveTargetColumn<TKey>(Expression<Func<T, TKey>> column)
    {
        Expression body = column.Body;
        if (body is UnaryExpression unary)
        {
            body = unary.Operand;
        }

        if (body is not MemberExpression member)
        {
            throw new ArgumentException("Expected a property access expression on the entity, like b => b.Title.", nameof(column));
        }

        TableColumn col = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == member.Member.Name)
            ?? throw new ArgumentException($"Property '{member.Member.Name}' is not mapped on {typeof(T).Name}.", nameof(column));

        return col;
    }

    private string TranslateBareSql(LambdaExpression lambda)
    {
        SQLVisitor visitor = new(database, new SQLiteCounters(), 0);

        Dictionary<string, Expression> columnExpressions = mapping.Columns.ToDictionary(
            c => c.PropertyInfo.Name,
            Expression (c) => new SQLiteExpression(c.PropertyType, visitor.Counters.IdentifierIndex++, c.Name));

        visitor.MethodArguments[lambda.Parameters[0]] = columnExpressions;

        Expression result = visitor.Visit(lambda.Body);
        if (result is not SQLiteExpression sqlExpr)
        {
            throw new ArgumentException($"Expression '{lambda}' could not be translated to SQL.", nameof(lambda));
        }

        return InlineParameters(sqlExpr.Sql, sqlExpr.Parameters);
    }

    private static string InlineParameters(string sql, SQLiteParameter[]? parameters)
    {
        if (parameters == null || parameters.Length == 0)
        {
            return sql;
        }

        foreach (SQLiteParameter parameter in parameters.OrderByDescending(p => p.Name.Length))
        {
            sql = sql.Replace(parameter.Name, FormatLiteral(parameter.Value));
        }
        return sql;
    }

    private static string FormatLiteral(object? value)
    {
        return value switch
        {
            null => "NULL",
            bool b => b ? "1" : "0",
            string s => "'" + s.Replace("'", "''") + "'",
            byte b => b.ToString(CultureInfo.InvariantCulture),
            sbyte b => b.ToString(CultureInfo.InvariantCulture),
            short b => b.ToString(CultureInfo.InvariantCulture),
            ushort b => b.ToString(CultureInfo.InvariantCulture),
            int b => b.ToString(CultureInfo.InvariantCulture),
            uint b => b.ToString(CultureInfo.InvariantCulture),
            long b => b.ToString(CultureInfo.InvariantCulture),
            ulong b => b.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString("R", CultureInfo.InvariantCulture),
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            decimal m => m.ToString(CultureInfo.InvariantCulture),
            _ => throw new NotSupportedException(
                $"Cannot inline value of type {value.GetType().Name} as a SQL literal. Use a simple constant in CHECK / Computed / partial-index expressions, or build the table with raw SQL."),
        };
    }

}
