namespace SQLite.Framework.Internals.Visitors;

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
    public SQLiteExpression? From { get; private set; }
    public Dictionary<ParameterExpression, Dictionary<string, Expression>> MethodArguments { get; set; } = [];
    public Dictionary<string, Expression> TableColumns { get; set; } = [];
    public CteRegistry? CteRegistry { get; set; }
    public Dictionary<ParameterExpression, (string Alias, Dictionary<string, Expression> Columns)> CteParameters { get; set; } = [];

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All entities have public properties.")]
    public void AssignValues(SQLiteExpression fromExpression, Dictionary<string, Expression> columns)
    {
        From = fromExpression;
        TableColumns = columns;
    }

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All entities have public properties.")]
    public void AssignTable(Type entityType, SQLiteExpression? sql = null)
    {
        char aliasChar = char.ToLowerInvariant(entityType.Name.FirstOrDefault(char.IsLetter, 't'));
        string alias = $"{aliasChar}{Counters.NextTableIndex(aliasChar)}";

        TableMapping tableMapping = Database.TableMapping(entityType);
        From = new SQLiteExpression(
            entityType,
            -1,
            $"{(sql != null ? $"({sql.Sql})" : $"\"{tableMapping.TableName}\"")} AS {alias}",
            sql?.Parameters
        );

        TableColumns = tableMapping.Columns
            .ToDictionary(f => f.PropertyInfo.Name, Expression (f) =>
            {
                string colSql = $"{alias}.{f.Name}";
                if (Database.Options.TypeConverters.TryGetValue(f.PropertyType, out ISQLiteTypeConverter? conv)
                    && conv.ColumnSqlExpression is { } colExpr)
                {
                    colSql = string.Format(colExpr, colSql);
                }
                return new SQLiteExpression(f.PropertyType, Counters.IdentifierIndex++, colSql);
            });
    }

    public SQLTranslator CloneDeeper(int innerLevel)
    {
        return new SQLTranslator(Database, Counters, innerLevel, true)
        {
            MethodArguments = MethodArguments,
            CteRegistry = CteRegistry,
            CteParameters = CteParameters
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
            if (node is UnaryExpression { NodeType: ExpressionType.Convert } convertNode)
            {
                object? innerValue = ExpressionHelpers.GetConstantValue(convertNode.Operand);
                Type targetType = Nullable.GetUnderlyingType(node.Type) ?? node.Type;
                if (innerValue?.GetType().IsEnum == true && targetType == Enum.GetUnderlyingType(innerValue.GetType()))
                {
                    constantValue = innerValue;
                }
            }

            sqlExpression = new SQLiteExpression(node.Type, Counters.IdentifierIndex++, $"@p{Counters.ParamIndex++}", constantValue);
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
}
