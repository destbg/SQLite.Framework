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
    public bool InCustomMethodTranslator { get; set; }
    public bool ClientEvalAllowed { get; set; }
    public bool ClientEvalUsed { get; set; }
    public bool SuppressUlongWindowOrderSplit { get; set; }
    public bool OmitTableAlias { get; set; }
    public bool SubqueryFreeSql { get; set; }
    public SQLiteExpression? From { get; internal set; }
    public Dictionary<ParameterExpression, Dictionary<string, Expression>> MethodArguments { get; set; } = [];
    public Dictionary<string, Expression> TableColumns { get; set; } = [];
    public CteRegistry? CteRegistry { get; set; }
    public Dictionary<ParameterExpression, CteSelfReference> CteParameters { get; set; } = [];
    public Dictionary<SQLiteExpression, SQLiteExpression>? DecimalCastIntern { get; set; }
    public Dictionary<(SQLiteExpression Source, string Member), SQLiteExpression>? JsonExtractIntern { get; set; }
    public Dictionary<ParameterExpression, RowColumnBinding> RowColumnPrefixes { get; } = [];
    public IReadOnlyDictionary<string, string>? SelectWrapFormats { get; set; }
    public IReadOnlyCollection<string>? ExcludedSelectColumns { get; set; }
    public Dictionary<Dictionary<string, Expression>, Dictionary<string, string?>> TableColumnPrefixes { get; set; } = [];
    public Dictionary<Dictionary<string, Expression>, HashSet<string>> ConstructedProjectionPaths { get; set; } = [];
    public HashSet<Dictionary<string, Expression>> OptionalRowColumns { get; set; } = [];
    public Dictionary<Dictionary<string, Expression>, HashSet<string>> OptionalRowPaths { get; set; } = [];
    public Dictionary<Dictionary<string, Expression>, Dictionary<string, Expression>> ConstructedProjectionNodes { get; set; } = [];

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

    public SQLiteExpression UnwrapDecimalCast(SQLiteExpression expression)
    {
        if (DecimalCastIntern == null)
        {
            return expression;
        }

        foreach (KeyValuePair<SQLiteExpression, SQLiteExpression> entry in DecimalCastIntern)
        {
            if (ReferenceEquals(entry.Value, expression))
            {
                return entry.Key;
            }
        }

        return expression;
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
            "json_extract(", source, $", {CommonHelpers.JsonExtractPathLiteral(CommonHelpers.JsonPathSegment(memberName))})",
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

    public void AssignTable(BaseSQLiteTable table)
    {
        AssignTableCore(table.Table, QualifiedTableName(table), sql: null);
    }

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All entities have public properties.")]
    public void AssignTable(Type entityType, SQLiteExpression sql)
    {
        TableMapping mapping = Database.TableMapping(entityType);
        AssignTableCore(mapping, $"\"{mapping.TableName}\"", sql);
    }

    public string QualifiedTableName(BaseSQLiteTable table)
    {
        string? schema = ResolveSchema(table);
        return schema != null
            ? $"\"{schema}\".\"{table.Table.TableName}\""
            : $"\"{table.Table.TableName}\"";
    }

    public SQLTranslator CloneDeeper(int innerLevel)
    {
        CteRegistry ??= new CteRegistry();
        return new SQLTranslator(Database, Counters, innerLevel, true)
        {
            MethodArguments = MethodArguments,
            CteRegistry = CteRegistry,
            CteParameters = CteParameters,
            TableColumnPrefixes = TableColumnPrefixes,
            ConstructedProjectionPaths = ConstructedProjectionPaths,
            OptionalRowColumns = OptionalRowColumns,
            OptionalRowPaths = OptionalRowPaths,
            ConstructedProjectionNodes = ConstructedProjectionNodes
        };
    }

    public SQLVisitor CloneForProjection(bool isInSelectProjection)
    {
        CteRegistry ??= new CteRegistry();
        return new SQLVisitor(Database, Counters, Level + 1)
        {
            MethodArguments = MethodArguments,
            TableColumnPrefixes = TableColumnPrefixes,
            ConstructedProjectionPaths = ConstructedProjectionPaths,
            OptionalRowColumns = OptionalRowColumns,
            OptionalRowPaths = OptionalRowPaths,
            ConstructedProjectionNodes = ConstructedProjectionNodes,
            ClientEvalAllowed = ClientEvalAllowed,
            IsInSelectProjection = isInSelectProjection,
            CteRegistry = CteRegistry
        };
    }

    public Expression ResolveMember(Expression node)
    {
        (string path, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(node);

        if (pe == null)
        {
            if (node is MemberExpression { Expression: not null } member)
            {
                Expression visited = Visit(member.Expression);
                if (visited is SQLiteExpression memberSql
                    && member.Member.Name == nameof(IGrouping<,>.Key)
                    && member.Expression.Type is { IsGenericType: true } groupingType
                    && groupingType.GetGenericTypeDefinition() == typeof(IGrouping<,>))
                {
                    return SQLiteExpression.Alias(member.Type, Counters.NextIdentifier(), memberSql, memberSql.Parameters).WithJsonSource();
                }

                return member.Update(visited);
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

            if (ResolveNestedConstructedMember(expressions, path) is { } nestedMember)
            {
                return nestedMember;
            }

            IEnumerable<KeyValuePair<string, Expression>> candidates = path.Length == 0
                ? expressions
                : expressions.Where(f => f.Key.StartsWith(path + ".", StringComparison.Ordinal));

            SQLiteExpression? sqlExpression = candidates
                .OrderBy(f => f.Key.Count(c => c == '.'))
                .ThenBy(f => f.Key.Length)
                .Select(f => f.Value)
                .OfType<SQLiteExpression>()
                .FirstOrDefault();

            if (sqlExpression != null)
            {
                return sqlExpression;
            }

            if (node is MemberExpression unresolvedMember)
            {
                return NotTranslatable(node,
                    $"The member '{unresolvedMember.Member.DeclaringType!.Name}.{unresolvedMember.Member.Name}' " +
                    "is not mapped to a database column, so it cannot be translated to SQL.");
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

    private Expression? ResolveNestedConstructedMember(Dictionary<string, Expression> expressions, string path)
    {
        int splitIndex = path.LastIndexOf('.');
        while (splitIndex > 0)
        {
            if (expressions.TryGetValue(path[..splitIndex], out Expression? baseExpression))
            {
                return baseExpression is ConditionalExpression or MemberInitExpression or NewExpression
                    ? VisitFoldedMemberPath(baseExpression, path[(splitIndex + 1)..])
                    : null;
            }

            if (ConstructedProjectionNodes.TryGetValue(expressions, out Dictionary<string, Expression>? nodes)
                && nodes.TryGetValue(path[..splitIndex], out Expression? nodeExpression))
            {
                return VisitFoldedMemberPath(nodeExpression, path[(splitIndex + 1)..]);
            }

            splitIndex = path.LastIndexOf('.', splitIndex - 1);
        }

        if (path.Length > 0
            && expressions.TryGetValue(string.Empty, out Expression? rootExpression)
            && rootExpression is ConditionalExpression or MemberInitExpression or NewExpression)
        {
            return VisitFoldedMemberPath(rootExpression, path);
        }

        if (path.Length > 0
            && ConstructedProjectionNodes.TryGetValue(expressions, out Dictionary<string, Expression>? rootNodes)
            && rootNodes.TryGetValue(string.Empty, out Expression? rootNode))
        {
            return VisitFoldedMemberPath(rootNode, path);
        }

        return null;
    }

    private Expression VisitFoldedMemberPath(Expression baseExpression, string memberPath)
    {
        Expression current = baseExpression;
        foreach (string segment in memberPath.Split('.'))
        {
            current = FoldConstructedMemberAccess(current, segment);
        }

        return Visit(current);
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

    private string? ResolveSchema(BaseSQLiteTable table)
    {
        if (table.SchemaName != null)
        {
            return table.SchemaName;
        }

        if (table.Database != Database)
        {
            if (Database.TryGetAttachedSchema(table.Database, out string? attachedSchema))
            {
                return attachedSchema;
            }

            throw new NotSupportedException(
                $"The query reads the table \"{table.Table.TableName}\" from another database that is not attached to this one. Attach it with AttachDatabase first.");
        }

        return null;
    }

    private void AssignTableCore(TableMapping tableMapping, string qualifiedName, SQLiteExpression? sql)
    {
        if (OmitTableAlias)
        {
            From = SQLiteExpression.Leaf(tableMapping.Type, -1, qualifiedName);
            TableColumns = BuildTableColumns(tableMapping, qualifiedName);
            return;
        }

        char aliasChar = char.ToLowerInvariant(tableMapping.Type.Name.FirstOrDefault(char.IsLetter, 't'));
        string alias = $"{aliasChar}{Counters.NextTableIndex(aliasChar)}";

        From = sql != null
            ? SQLiteExpression.Wrap(tableMapping.Type, -1, "(", sql, $") AS {alias}", sql.Parameters)
            : SQLiteExpression.Leaf(tableMapping.Type, -1, $"{qualifiedName} AS {alias}");

        TableColumns = BuildTableColumns(tableMapping, alias);
    }

    private Dictionary<string, Expression> BuildTableColumns(TableMapping tableMapping, string prefix)
    {
        Dictionary<string, Expression> columns = tableMapping.Columns
            .ToDictionary(f => f.PropertyInfo.Name, Expression (f) =>
            {
                string colSql = $"{prefix}.{IdentifierGuard.Quote(f.Name)}";
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

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Projection member types are rooted by the user query.")]
    private static Expression FoldConstructedMemberAccess(Expression expression, string memberName)
    {
        if (expression is ConditionalExpression conditional)
        {
            Expression? ifTrue = TryFoldConstructedBranch(conditional.IfTrue, memberName);
            Expression? ifFalse = TryFoldConstructedBranch(conditional.IfFalse, memberName);
            Type memberType = (ifTrue ?? ifFalse)!.Type;
            return Expression.Condition(
                conditional.Test,
                ifTrue ?? MakeDefaultConstant(memberType),
                ifFalse ?? MakeDefaultConstant(memberType));
        }

        if (expression is MemberInitExpression memberInitExpression)
        {
            MemberAssignment? binding = memberInitExpression.Bindings
                .OfType<MemberAssignment>()
                .FirstOrDefault(b => b.Member.Name == memberName);
            if (binding != null)
            {
                return binding.Expression;
            }

            expression = memberInitExpression.NewExpression;
        }

        if (expression is NewExpression newExpression)
        {
            int argumentIndex = ConstructorArgumentIndex(newExpression, memberName);
            if (argumentIndex < newExpression.Arguments.Count)
            {
                return newExpression.Arguments[argumentIndex];
            }

            if (newExpression.Arguments.Count == 0
                && TryGetSettableMemberType(newExpression.Type, memberName, out Type? unassignedType))
            {
                return MakeConstructedMemberConstant(newExpression, memberName, unassignedType);
            }
        }

        return Expression.PropertyOrField(expression, memberName);
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Projected shapes are rooted by user code.")]
    private static bool TryGetSettableMemberType(Type type, string memberName, [NotNullWhen(true)] out Type? memberType)
    {
        PropertyInfo? property = type.GetProperty(memberName);
        memberType = property?.PropertyType;
        return property is { CanWrite: true };
    }

    private static Expression? TryFoldConstructedBranch(Expression branch, string memberName)
    {
        return ExpressionHelpers.IsConstant(branch) && ExpressionHelpers.GetConstantValue(branch) == null
            ? null
            : FoldConstructedMemberAccess(branch, memberName);
    }

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "Value types always have a default constructor.")]
    private static ConstantExpression MakeDefaultConstant(Type type)
    {
        object? value = type.IsValueType && Nullable.GetUnderlyingType(type) == null
            ? Activator.CreateInstance(type)
            : null;
        return Expression.Constant(value, type);
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Constructed projection types are rooted by the user query.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Constructed projection types are rooted by the user query.")]
    private static ConstantExpression MakeConstructedMemberConstant(NewExpression newExpression, string memberName, Type memberType)
    {
        object instance = newExpression.Constructor == null
            ? Activator.CreateInstance(newExpression.Type)!
            : newExpression.Constructor.Invoke([]);
        object? value = newExpression.Type.GetProperty(memberName)!.GetValue(instance);
        return Expression.Constant(value, memberType);
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Constructed projection types are rooted by the user query.")]
    private static int ConstructorArgumentIndex(NewExpression newExpression, string memberName)
    {
        if (newExpression.Members != null)
        {
            return newExpression.Members.TakeWhile(m => m.Name != memberName).Count();
        }

        if (newExpression.Constructor == null)
        {
            return newExpression.Arguments.Count;
        }

        if (!TypeHelpers.HasPositionalIdentityMembers(newExpression.Type)
            && newExpression.Type.GetProperty(memberName) is not { CanWrite: false })
        {
            return newExpression.Arguments.Count;
        }

        return newExpression.Constructor.GetParameters()
            .TakeWhile(p => !string.Equals(p.Name, memberName, StringComparison.OrdinalIgnoreCase))
            .Count();
    }
}
