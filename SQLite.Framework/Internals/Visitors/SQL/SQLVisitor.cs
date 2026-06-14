namespace SQLite.Framework.Internals.Visitors.SQL;

/// <summary>
/// Handles the conversion of LINQ expressions to SQL expressions.
/// </summary>
/// <remarks>
/// This class is responsible for traversing the expression tree and converting it into a SQL representation.
/// The <see cref="QueryableVisitor" /> gets all the different LINQ methods and passes them to this
/// class for conversion to SQL.
/// Not all Expressions are converted to SQL, some are left as is so that the select method can execute
/// code both as SQL and C#.
/// </remarks>
internal partial class SQLVisitor : ExpressionVisitor
{
    public SQLVisitor(SQLiteDatabase database, SQLiteCounters counters, int level)
    {
        Database = database;
        Counters = counters;
        Level = level;
    }

    public SQLiteDatabase Database { get; }

    public SQLiteCounters Counters { get; }
    public int Level { get; }
    public bool IsInSelectProjection { get; set; }
    public bool ClientEvalAllowed { get; set; }
    public bool ClientEvalUsed { get; set; }
    public bool OmitTableAlias { get; set; }
    public SQLiteExpression? From { get; internal set; }
    public Dictionary<ParameterExpression, Dictionary<string, Expression>> MethodArguments { get; set; } = [];
    public Dictionary<string, Expression> TableColumns { get; set; } = [];
    public CteRegistry? CteRegistry { get; set; }
    public Dictionary<ParameterExpression, (string Alias, Dictionary<string, Expression> Columns)> CteParameters { get; set; } = [];
    public Dictionary<SQLiteExpression, SQLiteExpression>? DecimalCastIntern { get; set; }
    public Dictionary<(SQLiteExpression Source, string Member), SQLiteExpression>? JsonExtractIntern { get; set; }
    public Dictionary<ParameterExpression, string?> RowColumnPrefixes { get; } = [];
    public Dictionary<Dictionary<string, Expression>, Dictionary<string, string?>> TableColumnPrefixes { get; set; } = [];

    public SQLiteExpression InternDecimalCast(SQLiteExpression source)
    {
        DecimalCastIntern ??= new();
        if (DecimalCastIntern.TryGetValue(source, out SQLiteExpression? cached))
        {
            return cached;
        }

        SQLiteExpression cast = SQLiteExpression.Wrap(source.Type, Counters.NextIdentifier(), "CAST(", source, " AS REAL)", source.Parameters);
        DecimalCastIntern[source] = cast;
        return cast;
    }

    public SQLiteExpression InternJsonExtract(SQLiteExpression source, string memberName, Type resultType)
    {
        JsonExtractIntern ??= new();
        (SQLiteExpression Source, string Member) key = (source, memberName);
        if (JsonExtractIntern.TryGetValue(key, out SQLiteExpression? cached))
        {
            return cached;
        }

        SQLiteExpression extracted = SQLiteExpression.Wrap(resultType, Counters.NextIdentifier(),
            "json_extract(", source, $", '$.{memberName}')",
            source.Parameters)
        .WithJsonSource();
        JsonExtractIntern[key] = extracted;
        return extracted;
    }

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All entities have public properties.")]
    public void AssignValues(SQLiteExpression fromExpression, Dictionary<string, Expression> columns)
    {
        From = fromExpression;
        TableColumns = columns;
    }

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All entities have public properties.")]
    public void AssignTable(Type entityType, SQLiteExpression? sql = null)
    {
        TableMapping tableMapping = Database.TableMapping(entityType);
        string tableName = tableMapping.TableName;

        if (OmitTableAlias)
        {
            From = SQLiteExpression.Leaf(entityType, -1, $"\"{tableName}\"");
            TableColumns = BuildTableColumns(tableMapping, prefix: null);
            return;
        }

        char aliasChar = char.ToLowerInvariant(entityType.Name.FirstOrDefault(char.IsLetter, 't'));
        string alias = $"{aliasChar}{Counters.NextTableIndex(aliasChar)}";

        From = sql != null
            ? SQLiteExpression.Wrap(entityType, -1, "(", sql, $") AS {alias}", sql.Parameters)
            : SQLiteExpression.Leaf(entityType, -1, $"\"{tableName}\" AS {alias}");

        TableColumns = BuildTableColumns(tableMapping, alias);
    }

    public SQLTranslator CloneDeeper(int innerLevel)
    {
        CteRegistry ??= new CteRegistry();
        return new SQLTranslator(Database, Counters, innerLevel, true)
        {
            MethodArguments = MethodArguments,
            CteRegistry = CteRegistry,
            CteParameters = CteParameters,
            TableColumnPrefixes = TableColumnPrefixes
        };
    }

    public Expression ResolveMember(Expression node)
    {
        (string path, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(node);

        if (pe == null)
        {
            if (node is MemberExpression { Expression: not null } member)
            {
                return member.Update(Visit(member.Expression));
            }

            return node;
        }

        if (MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? expressions))
        {
            if (expressions.TryGetValue(path, out Expression? expression))
            {
                return expression;
            }

            if (ResolvePrimaryKeyColumn(node.Type, path, expressions) is { } primaryKeyColumn)
            {
                return primaryKeyColumn;
            }

            SQLiteExpression? sqlExpression = expressions
                .OrderBy(f => f.Key.Count(c => c == '.'))
                .ThenBy(f => f.Key.Length)
                .Select(f => f.Value)
                .OfType<SQLiteExpression>()
                .FirstOrDefault();

            if (sqlExpression != null)
            {
                return sqlExpression;
            }
        }

        throw new NotSupportedException($"Cannot translate expression {node}");
    }

    public ResolvedModel ResolveExpression(Expression node)
    {
        bool isConstant = ExpressionHelpers.IsConstant(node);
        object? constantValue;
        SQLiteExpression? sqlExpression;
        Expression resolvedExpression;

        if (isConstant)
        {
            constantValue = ExpressionHelpers.GetConstantValue(node);
            if (node is UnaryExpression convertNode
                && ExpressionHelpers.GetConstantValue(convertNode.Operand) is Enum enumValue)
            {
                constantValue = enumValue;
            }

            sqlExpression = SQLiteExpression.Leaf(node.Type, Counters.NextIdentifier(), Counters.NextParamName(), constantValue);
            resolvedExpression = node;
        }
        else
        {
            constantValue = null;
            resolvedExpression = Visit(node);
            if (resolvedExpression is SQLiteExpression sqlResolvedExpression)
            {
                sqlExpression = sqlResolvedExpression;
            }
            else
            {
                sqlExpression = null;
            }
        }

        return new ResolvedModel
        {
            IsConstant = isConstant,
            Constant = constantValue,
            SQLiteExpression = sqlExpression,
            Expression = resolvedExpression
        };
    }

    private SQLiteExpression? ResolvePrimaryKeyColumn(Type entityType, string path, Dictionary<string, Expression> expressions)
    {
        if (!Database.TryGetCachedTableMapping(entityType, out TableMapping? mapping))
        {
            return null;
        }

        string prefix = path.Length == 0 ? "" : path + ".";
        foreach (TableColumn column in mapping.Columns
                     .Where(c => c.IsPrimaryKey)
                     .OrderBy(c => c.PrimaryKeyOrder))
        {
            if (expressions.TryGetValue(prefix + column.PropertyInfo.Name, out Expression? expression)
                && expression is SQLiteExpression sqlExpression)
            {
                return sqlExpression;
            }
        }

        return null;
    }

    private Dictionary<string, Expression> BuildTableColumns(TableMapping tableMapping, string? prefix)
    {
        Dictionary<string, Expression> columns = tableMapping.Columns
            .ToDictionary(f => f.PropertyInfo.Name, Expression (f) =>
            {
                string quotedName = IdentifierGuard.Quote(f.Name);
                string colSql = prefix != null ? $"{prefix}.{quotedName}" : quotedName;
                if (Database.Options.TypeConverters.TryGetValue(f.PropertyType, out ISQLiteTypeConverter? conv)
                    && conv.ColumnSqlExpression is { } colExpr)
                {
                    colSql = string.Format(colExpr, colSql);
                }
                return SQLiteExpression.Leaf(f.PropertyType, Counters.NextIdentifier(), colSql);
            });

        TableColumnPrefixes[columns] = new Dictionary<string, string?> { [string.Empty] = prefix };
        return columns;
    }
}
