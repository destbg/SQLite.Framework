using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Models;

namespace SQLite.Framework.Internals;

internal class SQLVisitor
{
    private readonly SQLiteDatabase database;
    private readonly MethodHandler methodHandler;

    public SQLVisitor(SQLiteDatabase database, Dictionary<string, object?> parameters, IndexWrapper paramIndex, IndexWrapper tableIndex, int level)
    {
        this.database = database;
        Parameters = parameters;
        ParamIndex = paramIndex;
        TableIndex = tableIndex;
        Level = level;
        methodHandler = new(this);
    }

    public List<JoinInfo> Joins { get; } = new();
    public List<string> Wheres { get; } = new();
    public List<string> OrderBys { get; } = new();
    public List<string> Selects { get; } = new();
    public List<(string Sql, bool All)> Unions { get; } = new();
    public Dictionary<Type, string> TableAliases { get; } = new();
    public Dictionary<string, object?> Parameters { get; }
    public IndexWrapper ParamIndex { get; }
    public IndexWrapper TableIndex { get; }
    public int Level { get; }
    public string? From { get; set; }
    public int? Take { get; set; }
    public int? Skip { get; set; }
    public bool IsAny { get; set; }
    public bool IsAll { get; set; }
    public bool IsDistinct { get; set; }
    public bool ThrowOnEmpty { get; set; }
    public bool ThrowOnMoreThanOne { get; set; }
    public Dictionary<ParameterExpression, Dictionary<string, ColumnMapping>> MethodArguments { get; set; } = [];
    public Dictionary<string, ColumnMapping> TableColumns { get; set; } = [];

    public SQLModel Build()
    {
        if (From == null)
        {
            throw new InvalidOperationException("Could not identify FROM clause.");
        }

        return new SQLModel
        {
            Parameters = Parameters,
            From = From,
            Joins = Joins,
            Selects = Selects,
            Unions = Unions,
            Skip = Skip,
            Take = Take,
            Wheres = Wheres,
            OrderBys = OrderBys,
            IsAny = IsAny,
            IsAll = IsAll,
            IsDistinct = IsDistinct,
            ThrowOnEmpty = ThrowOnEmpty,
            ThrowOnMoreThanOne = ThrowOnMoreThanOne,
        };
    }

    public string Visit(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        string result = expression.NodeType switch
        {
            ExpressionType.Add => VisitBinary((BinaryExpression)expression),
            ExpressionType.And => VisitBinary((BinaryExpression)expression),
            ExpressionType.AndAlso => VisitBinary((BinaryExpression)expression),
            ExpressionType.ArrayIndex => VisitBinary((BinaryExpression)expression),
            ExpressionType.Assign => VisitBinary((BinaryExpression)expression),
            ExpressionType.Call => VisitMethodCall((MethodCallExpression)expression),
            ExpressionType.Conditional => VisitConditional((ConditionalExpression)expression),
            ExpressionType.Constant => VisitConstant((ConstantExpression)expression),
            ExpressionType.Convert => VisitUnary((UnaryExpression)expression),
            ExpressionType.Divide => VisitBinary((BinaryExpression)expression),
            ExpressionType.Equal => VisitBinary((BinaryExpression)expression),
            ExpressionType.ExclusiveOr => VisitBinary((BinaryExpression)expression),
            ExpressionType.GreaterThan => VisitBinary((BinaryExpression)expression),
            ExpressionType.GreaterThanOrEqual => VisitBinary((BinaryExpression)expression),
            ExpressionType.LeftShift => VisitBinary((BinaryExpression)expression),
            ExpressionType.LessThan => VisitBinary((BinaryExpression)expression),
            ExpressionType.LessThanOrEqual => VisitBinary((BinaryExpression)expression),
            ExpressionType.MemberAccess => VisitMember((MemberExpression)expression),
            ExpressionType.Modulo => VisitBinary((BinaryExpression)expression),
            ExpressionType.Multiply => VisitBinary((BinaryExpression)expression),
            ExpressionType.Negate => VisitUnary((UnaryExpression)expression),
            ExpressionType.Not => VisitUnary((UnaryExpression)expression),
            ExpressionType.NotEqual => VisitBinary((BinaryExpression)expression),
            ExpressionType.Or => VisitBinary((BinaryExpression)expression),
            ExpressionType.OrElse => VisitBinary((BinaryExpression)expression),
            ExpressionType.Power => VisitBinary((BinaryExpression)expression),
            ExpressionType.Quote => VisitUnary((UnaryExpression)expression),
            ExpressionType.RightShift => VisitBinary((BinaryExpression)expression),
            ExpressionType.Subtract => VisitBinary((BinaryExpression)expression),
            ExpressionType.TypeAs => VisitUnary((UnaryExpression)expression),
            ExpressionType.UnaryPlus => VisitUnary((UnaryExpression)expression),
            ExpressionType.OnesComplement => VisitUnary((UnaryExpression)expression),
            ExpressionType.AddChecked => VisitBinary((BinaryExpression)expression),
            ExpressionType.ArrayLength => VisitUnary((UnaryExpression)expression),
            ExpressionType.Coalesce => VisitBinary((BinaryExpression)expression),
            ExpressionType.ConvertChecked => VisitUnary((UnaryExpression)expression),
            ExpressionType.MultiplyChecked => VisitBinary((BinaryExpression)expression),
            ExpressionType.NegateChecked => VisitUnary((UnaryExpression)expression),
            ExpressionType.SubtractChecked => VisitBinary((BinaryExpression)expression),
            ExpressionType.Decrement => VisitUnary((UnaryExpression)expression),
            ExpressionType.Increment => VisitUnary((UnaryExpression)expression),
            ExpressionType.Throw => VisitUnary((UnaryExpression)expression),
            ExpressionType.Unbox => VisitUnary((UnaryExpression)expression),
            ExpressionType.IsTrue => VisitBinary((BinaryExpression)expression),
            ExpressionType.IsFalse => VisitBinary((BinaryExpression)expression),
            _ => throw new NotSupportedException($"The expression type '{expression.GetType()}' is not supported.")
        };

        return result ?? throw new NotSupportedException($"The expression '{expression}' is not supported.");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "All entities have public properties.")]
    public void AssignTable(Type entityType)
    {
        string alias = $"{entityType.Name.ToLowerInvariant()[..1]}{TableIndex.Index++}";
        TableAliases[entityType] = alias;

        TableMapping tableMapping = database.TableMapping(entityType);
        From = $"\"{tableMapping.TableName}\" AS {alias}";

        TableColumns = tableMapping.Columns
            .ToDictionary(f => f.PropertyInfo.Name, f => new ColumnMapping(alias, f.Name, f.PropertyInfo.Name));
    }

    public SQLTranslator CloneDeeper(int innerLevel)
    {
        return new SQLTranslator(database, Parameters, ParamIndex, TableIndex, innerLevel)
        {
            MethodArguments = MethodArguments
        };
    }

    private string VisitBinary(BinaryExpression node)
    {
        if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
        {
            string op = node.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
            return $"{CommonHelpers.BracketIfNeeded(Visit(node.Left))} {op} {CommonHelpers.BracketIfNeeded(Visit(node.Right))}";
        }
        else if (node.NodeType is ExpressionType.Coalesce)
        {
            return $"COALESCE({CommonHelpers.BracketIfNeeded(Visit(node.Left))}, {CommonHelpers.BracketIfNeeded(Visit(node.Right))})";
        }

        string sqlOp = node.NodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Multiply => "*",
            ExpressionType.Divide => "/",
            _ => throw new NotSupportedException($"Unsupported binary op {node.NodeType}")
        };

        object? leftObject = node.Left is MemberExpression { Expression: ConstantExpression } or ConstantExpression
            ? CommonHelpers.GetConstantValue(node.Left)
            : string.Empty;

        object? rightObject = node.Right is MemberExpression { Expression: ConstantExpression } or ConstantExpression
            ? CommonHelpers.GetConstantValue(node.Right)
            : string.Empty;

        string left;
        string right;
        if (leftObject == null || rightObject == null)
        {
            if (node.NodeType == ExpressionType.Equal)
            {
                sqlOp = "IS";
            }
            else if (node.NodeType == ExpressionType.NotEqual)
            {
                sqlOp = "IS NOT";
            }

            if (leftObject == null)
            {
                right = "NULL";
                left = CommonHelpers.BracketIfNeeded(Visit(node.Right));
            }
            else
            {
                left = CommonHelpers.BracketIfNeeded(Visit(node.Left));
                right = "NULL";
            }
        }
        else
        {
            left = CommonHelpers.BracketIfNeeded(Visit(node.Left));
            right = CommonHelpers.BracketIfNeeded(Visit(node.Right));
        }

        return $"{left} {sqlOp} {right}";
    }

    [UnconditionalSuppressMessage("Trimming", "IL2062", Justification = "All types have public properties.")]
    [UnconditionalSuppressMessage("Trimming", "IL2065", Justification = "The type is an entity.")]
    private string VisitConstant(ConstantExpression node)
    {
        // detect the root: SQLiteTable<T>
        Type? qt = node.Value?.GetType();
        if (qt is { IsGenericType: true } && qt.GetGenericTypeDefinition() == typeof(SQLiteTable<>))
        {
            Type entityType = qt.GetGenericArguments()[0];
            AssignTable(entityType);
            return From!;
        }

        string pName = $"@p{ParamIndex.Index++}";
        Parameters[pName] = CommonHelpers.GetConstantValue(node);
        return pName;
    }

    private string VisitConditional(ConditionalExpression node)
    {
        string ifTrue = CommonHelpers.BracketIfNeeded(Visit(node.IfTrue));
        string ifFalse = CommonHelpers.BracketIfNeeded(Visit(node.IfFalse));
        string test = CommonHelpers.BracketIfNeeded(Visit(node.Test));

        return $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END";
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "All entities have public properties.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The type is an entity.")]
    private string VisitMember(MemberExpression node)
    {
        if (node.Expression is ConstantExpression)
        {
            // detect the root: SQLiteTable<T>
            object? value = CommonHelpers.GetConstantValue(node);
            if (value is SQLiteTable table)
            {
                AssignTable(table.ElementType);
                return From!;
            }

            string pName = $"@p{ParamIndex.Index++}";
            Parameters[pName] = value;
            return pName;
        }

        return ResolveMember(node);
    }

    private string VisitUnary(UnaryExpression node)
    {
        string sql = Visit(node.Operand);
        return node.NodeType == ExpressionType.Convert ? sql : $"({sql})";
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "All types have public properties.")]
    private string VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(string))
        {
            return methodHandler.HandleStringExtension(node);
        }
        else if (node.Method.DeclaringType == typeof(Math))
        {
            return methodHandler.HandleMathExtension(node);
        }
        else if (node.Method.DeclaringType == typeof(DateTime))
        {
            return methodHandler.HandleDateExtension(node);
        }
        else if (node.Method.DeclaringType == typeof(Guid))
        {
            return methodHandler.HandleGuidExtension(node);
        }
        else if (node.Method.DeclaringType == typeof(Queryable))
        {
            return methodHandler.HandleQueryableExtension(node);
        }
        else if (node.Object != null)
        {
            object? value = CommonHelpers.GetConstantValue(node.Object);

            if (value is IEnumerable enumerable)
            {
                return methodHandler.HandleEnumerableExtension(node, enumerable);
            }
        }
        else if (node.Arguments.Count > 0)
        {
            object? value = CommonHelpers.GetConstantValue(node.Arguments[0]);

            if (value is IEnumerable enumerable)
            {
                return methodHandler.HandleEnumerableExtension(node, enumerable);
            }
        }

        throw new NotSupportedException($"Unsupported method {node.Method.Name}");
    }

    public void BuildSelect(Expression expr, string? prefix)
    {
        if (expr is MemberInitExpression mi)
        {
            // new TDto { Prop = ..., Prop2 = ... }
            foreach (MemberAssignment bind in mi.Bindings.Cast<MemberAssignment>())
            {
                // 1) Nested DTO: e.g. Author = new AuthorDTO { Id = ..., Name = ... }
                if (bind.Expression is MemberInitExpression nested && bind.Member is PropertyInfo pi)
                {
                    BuildSelect(nested, pi.Name);
                }
                else
                {
                    string path = $"{(prefix != null ? $"{prefix}." : string.Empty)}{bind.Member.Name}";

                    ColumnMapping mapping = TableColumns[path];
                    Selects.Add($"{CommonHelpers.BracketIfNeeded(mapping.Sql)} AS \"{(prefix != null ? $"{prefix}." : string.Empty)}{bind.Member.Name}\"");
                }
            }
        }
        else if (expr is NewExpression nex)
        {
            // new { Prop = ..., Prop2 = ... }
            foreach (Expression bind in nex.Arguments)
            {
                // 1) Nested DTO: e.g. Author = new AuthorDTO { Id = ..., Name = ... }
                if (bind is MemberExpression memberExpression)
                {
                    string path = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberExpression.Member.Name}";

                    ColumnMapping mapping = TableColumns[path];
                    Selects.Add($"{CommonHelpers.BracketIfNeeded(mapping.Sql)} AS \"{(prefix != null ? $"{prefix}." : string.Empty)}{memberExpression.Member.Name}\"");
                }
                else
                {
                    throw new NotSupportedException($"Unsupported member expression {nex}");
                }
            }
        }
        else if (expr is MemberExpression me)
        {
            if (CommonHelpers.IsSimple(me.Type))
            {
                (string path, ParameterExpression _) = CommonHelpers.ResolveParameterPath(me);

                if (TableColumns.TryGetValue(path, out ColumnMapping? tableColumn))
                {
                    Selects.Add($"{CommonHelpers.BracketIfNeeded(tableColumn.Sql)} AS \"{(prefix != null ? $"{prefix}." : string.Empty)}{me.Member.Name}\"");
                }
            }
            else
            {
                foreach (KeyValuePair<string, ColumnMapping> tableColumn in TableColumns)
                {
                    string sql = tableColumn.Value.Sql;
                    Selects.Add($"{CommonHelpers.BracketIfNeeded(sql)} AS \"{(prefix != null ? $"{prefix}." : string.Empty)}{tableColumn.Value.PropertyName}\"");
                }
            }
        }
        else if (TableColumns.TryGetValue(string.Empty, out ColumnMapping? tableColumn))
        {
            Selects.Add(CommonHelpers.BracketIfNeeded(tableColumn.Sql));
        }
        else if (expr is ParameterExpression)
        {
            foreach (KeyValuePair<string, ColumnMapping> tableColumn2 in TableColumns)
            {
                string sql = tableColumn2.Value.Sql;
                Selects.Add($"{CommonHelpers.BracketIfNeeded(sql)} AS \"{(prefix != null ? $"{prefix}." : string.Empty)}{tableColumn2.Key}\"");
            }
        }
        else
        {
            throw new NotSupportedException("Only simple .Select(new DTO { … }) or .Select(f => f.Id) is supported");
        }
    }

    public string ResolveMember(Expression expression)
    {
        (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(expression);

        if (MethodArguments.TryGetValue(pe, out Dictionary<string, ColumnMapping>? tableColumns))
        {
            if (tableColumns.TryGetValue(path, out ColumnMapping? tableColumn))
            {
                return tableColumn.Sql;
            }
        }

        throw new NotSupportedException($"Cannot translate expression {expression}");
    }

    public Dictionary<string, ColumnMapping> ResolveResultAlias(LambdaExpression lambdaExpression, Expression body, string? prefix = null)
    {
        Dictionary<string, ColumnMapping> result = [];

        if (body is NewExpression newExpression)
        {
            if (newExpression.Arguments.Count > 0)
            {
                foreach (Expression argument in newExpression.Arguments)
                {
                    if (argument is ParameterExpression parameterExpression)
                    {
                        string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{parameterExpression.Name}";
                        Dictionary<string, ColumnMapping> parameterTableColumns = MethodArguments[parameterExpression];

                        foreach (KeyValuePair<string, ColumnMapping> tableColumn in parameterTableColumns)
                        {
                            result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                        }
                    }
                    else if (argument is MemberExpression memberExpression)
                    {
                        string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberExpression.Member.Name}";
                        (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(memberExpression);

                        Dictionary<string, ColumnMapping> parameterTableColumns = MethodArguments[pe];

                        if (CommonHelpers.IsSimple(memberExpression.Type))
                        {
                            result.Add(alias, parameterTableColumns[path]);
                        }
                        else
                        {
                            foreach (KeyValuePair<string, ColumnMapping> tableColumn in parameterTableColumns)
                            {
                                if (tableColumn.Key.StartsWith(path))
                                {
                                    result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                                }
                            }
                        }
                    }
                    else if (argument is ParameterExpression parameterExpr)
                    {
                        string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{parameterExpr.Name}";
                        (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(parameterExpr);

                        Dictionary<string, ColumnMapping> parameterTableColumns = MethodArguments[pe];

                        if (CommonHelpers.IsSimple(parameterExpr.Type))
                        {
                            result.Add(alias, parameterTableColumns[path]);
                        }
                        else
                        {
                            foreach (KeyValuePair<string, ColumnMapping> tableColumn in parameterTableColumns)
                            {
                                if (tableColumn.Key.StartsWith(path))
                                {
                                    result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"Unsupported member expression {argument}");
                    }
                }
            }
            else if (newExpression.Members == null)
            {
                throw new NotSupportedException("Cannot translate expression");
            }
            else
            {
                foreach (MemberInfo memberInfo in newExpression.Members)
                {
                    string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberInfo.Name}";
                    Type propertyType = memberInfo is PropertyInfo pi ? pi.PropertyType : ((FieldInfo)memberInfo).FieldType;

                    ParameterExpression expression = lambdaExpression.Parameters
                        .First(f => (f.Name == memberInfo.Name && f.Type == propertyType) || f.Type == propertyType);

                    (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(expression);

                    Dictionary<string, ColumnMapping> parameterTableColumns = MethodArguments[pe];

                    foreach (KeyValuePair<string, ColumnMapping> tableColumn in parameterTableColumns)
                    {
                        if (tableColumn.Key.StartsWith(path))
                        {
                            result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                        }
                    }
                }
            }
        }
        else if (body is MemberInitExpression memberInitExpression)
        {
            foreach (MemberAssignment memberAssignment in memberInitExpression.Bindings.Cast<MemberAssignment>())
            {
                if (memberAssignment.Expression is MemberInitExpression or NewExpression)
                {
                    string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberAssignment.Member.Name}";
                    Dictionary<string, ColumnMapping> innerResult = ResolveResultAlias(lambdaExpression, memberAssignment.Expression, alias);

                    foreach (KeyValuePair<string, ColumnMapping> tableColumn in innerResult)
                    {
                        result.Add(tableColumn.Key, tableColumn.Value);
                    }
                }
                else if (memberAssignment.Expression is ParameterExpression parameterExpression)
                {
                    string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberAssignment.Member.Name}";
                    Dictionary<string, ColumnMapping> parameterTableColumns = MethodArguments[parameterExpression];

                    foreach (KeyValuePair<string, ColumnMapping> tableColumn in parameterTableColumns)
                    {
                        result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                    }
                }
                else if (memberAssignment.Expression is MemberExpression or ParameterExpression)
                {
                    string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberAssignment.Member.Name}";
                    (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(memberAssignment.Expression);

                    Dictionary<string, ColumnMapping> parameterTableColumns = MethodArguments[pe];

                    if (CommonHelpers.IsSimple(memberAssignment.Expression.Type))
                    {
                        result.Add(alias, parameterTableColumns[path]);
                    }
                    else
                    {
                        foreach (KeyValuePair<string, ColumnMapping> tableColumn in parameterTableColumns)
                        {
                            if (tableColumn.Key.StartsWith(path))
                            {
                                result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                            }
                        }
                    }
                }
                else
                {
                    string alias = $"{(prefix != null ? $"{prefix}." : string.Empty)}{memberAssignment.Member.Name}";
                    SQLVisitor innerVisitor = new(database, Parameters, ParamIndex, TableIndex, Level + 1)
                    {
                        MethodArguments = MethodArguments
                    };
                    string sql = innerVisitor.Visit(memberAssignment.Expression);
                    result.Add(alias, new ColumnMapping(sql));
                }
            }
        }
        else if (body is MemberExpression memberExpression)
        {
            if (CommonHelpers.IsSimple(memberExpression.Type))
            {
                (string path, ParameterExpression _) = CommonHelpers.ResolveParameterPath(body);

                ColumnMapping columnMapping = TableColumns[path];
                result.Add(memberExpression.Member.Name, columnMapping);
            }
            else
            {
                (string path, ParameterExpression _) = CommonHelpers.ResolveParameterPath(body);

                foreach (KeyValuePair<string, ColumnMapping> tableColumn in TableColumns)
                {
                    if (tableColumn.Key.StartsWith(path))
                    {
                        string newPath = tableColumn.Key[(path.Length + 1)..];
                        result.Add(newPath, tableColumn.Value);
                    }
                }
            }
        }
        else if (body is ParameterExpression pe)
        {
            Dictionary<string, ColumnMapping> tableColumns = MethodArguments[pe];

            foreach (KeyValuePair<string, ColumnMapping> tableColumn in tableColumns)
            {
                result.Add(tableColumn.Key, tableColumn.Value);
            }
        }
        else
        {
            SQLVisitor innerVisitor = new(database, Parameters, ParamIndex, TableIndex, Level + 1)
            {
                MethodArguments = MethodArguments
            };
            string sql = innerVisitor.Visit(body);
            result.Add(string.Empty, new ColumnMapping(sql));
        }

        return result;
    }
}