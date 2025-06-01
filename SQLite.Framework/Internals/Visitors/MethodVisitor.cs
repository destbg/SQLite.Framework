using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;

namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Handles the conversion of common object methods to their respective SQL expressions.
/// </summary>
internal class MethodVisitor
{
    private readonly SQLVisitor visitor;

    public MethodVisitor(SQLVisitor visitor)
    {
        this.visitor = visitor;
    }

    public Expression HandleStringMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null))
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            switch (node.Method.Name)
            {
                case nameof(string.Contains):
                {
                    return ResolveLike(node.Method, obj.SQLExpression, arguments, value => $"%{value}%", valueSql => $"'%'||{valueSql}||'%'");
                }
                case nameof(string.StartsWith):
                {
                    return ResolveLike(node.Method, obj.SQLExpression, arguments, value => $"{value}%", valueSql => $"{valueSql}||'%'");
                }
                case nameof(string.EndsWith):
                {
                    return ResolveLike(node.Method, obj.SQLExpression, arguments, value => $"%{value}", valueSql => $"'%'||{valueSql}");
                }
                case nameof(string.Equals):
                {
                    SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!);
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex++,
                        $"{obj.Sql} = {arguments[0].Sql}",
                        parameters
                    );
                }
                case nameof(string.IndexOf):
                {
                    SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!);
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex++,
                        $"INSTR({obj.Sql}, {arguments[0].Sql})",
                        parameters
                    );
                }
                case nameof(string.Replace):
                {
                    SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!, arguments[1].SQLExpression!);
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex++,
                        $"REPLACE({obj.Sql}, {arguments[0].Sql}, {arguments[1].Sql})",
                        parameters
                    );
                }
                case nameof(string.Trim):
                {
                    return ResolveTrim(node, obj.SQLExpression, arguments, "TRIM");
                }
                case nameof(string.TrimStart):
                {
                    return ResolveTrim(node, obj.SQLExpression, arguments, "LTRIM");
                }
                case nameof(string.TrimEnd):
                {
                    return ResolveTrim(node, obj.SQLExpression, arguments, "RTRIM");
                }
                case nameof(string.Substring):
                {
                    if (node.Arguments.Count == 2)
                    {
                        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!, arguments[1].SQLExpression!);
                        return new SQLExpression(
                            node.Method.ReturnType,
                            visitor.IdentifierIndex++,
                            $"SUBSTR({obj.Sql}, {arguments[0].Sql}, {arguments[1].Sql})",
                            parameters
                        );
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!);
                        return new SQLExpression(
                            node.Method.ReturnType,
                            visitor.IdentifierIndex++,
                            $"SUBSTR({obj.Sql}, {arguments[0].Sql})",
                            parameters
                        );
                    }
                }
                case nameof(string.ToUpper):
                case nameof(string.ToUpperInvariant):
                {
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex++,
                        $"UPPER({obj.Sql})",
                        obj.Parameters
                    );
                }
                case nameof(string.ToLower):
                case nameof(string.ToLowerInvariant):
                {
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex++,
                        $"LOWER({obj.Sql})",
                        obj.Parameters
                    );
                }
            }
        }
        else if (CheckConstantMethod<string>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        return node.Method.Name switch
        {
            nameof(string.IsNullOrEmpty) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"({arguments[0].Sql} IS NULL OR {arguments[0].Sql} = '')",
                arguments[0].Parameters
            ),
            nameof(string.IsNullOrWhiteSpace) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"({arguments[0].Sql} IS NULL OR TRIM({arguments[0].Sql}, ' ') = '')",
                arguments[0].Parameters
            ),
            nameof(string.Concat) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                string.Join(" || ", arguments.Select(f => f.Sql)),
                CommonHelpers.CombineParameters(arguments.Select(f => f.SQLExpression!).ToArray())
            ),
            _ => node
        };
    }

    public Expression HandleMathMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (CheckConstantMethod<double>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(arguments.Select(f => f.SQLExpression!).ToArray());

        return node.Method.Name switch
        {
            nameof(Math.Min) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"(CASE WHEN {arguments[0].Sql} < {arguments[1].Sql} THEN {arguments[0].Sql} ELSE {arguments[1].Sql} END)",
                parameters
            ),
            nameof(Math.Max) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"(CASE WHEN {arguments[0].Sql} > {arguments[1].Sql} THEN {arguments[0].Sql} ELSE {arguments[1].Sql} END)",
                parameters
            ),
            nameof(Math.Abs) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"ABS({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Round) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"ROUND({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Ceiling) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"CEIL({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Floor) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"FLOOR({arguments[0].Sql})",
                parameters
            ),
            _ => node
        };
    }

    public Expression HandleDateTimeMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null))
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(DateTime.Add) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, 1),
                nameof(DateTime.AddYears) => ResolveRelativeDate(node.Method, obj.SQLExpression, arguments, "years"),
                nameof(DateTime.AddMonths) => ResolveRelativeDate(node.Method, obj.SQLExpression, arguments, "months"),
                nameof(DateTime.AddDays) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerDay),
                nameof(DateTime.AddHours) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerHour),
                nameof(DateTime.AddMinutes) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerMinute),
                nameof(DateTime.AddSeconds) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerSecond),
                nameof(DateTime.AddMilliseconds) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerMillisecond),
                nameof(DateTime.AddMicroseconds) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerMicrosecond),
                nameof(DateTime.AddTicks) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, 1),
                nameof(DateTime.Subtract) => new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex++,
                    $"{obj.Sql} - {arguments[0].Sql}",
                    CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!)
                ),
                _ => node
            };
        }
        else if (CheckConstantMethod<DateTime>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        return node.Method.Name switch
        {
            nameof(DateTime.Parse) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"@p{visitor.ParamIndex.Index++}",
                DateTime.Parse((string)arguments[0].Constant!).Ticks
            ),
            nameof(DateTime.FromBinary) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"@p{visitor.ParamIndex.Index++}",
                DateTime.FromBinary((long)arguments[0].Constant!).Ticks
            ),
            _ => node
        };
    }

    public Expression HandleDateTimeOffsetMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null))
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(DateTimeOffset.Add) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, 1),
                nameof(DateTimeOffset.AddYears) => ResolveRelativeDate(node.Method, obj.SQLExpression, arguments, "years"),
                nameof(DateTimeOffset.AddMonths) => ResolveRelativeDate(node.Method, obj.SQLExpression, arguments, "months"),
                nameof(DateTimeOffset.AddDays) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerDay),
                nameof(DateTimeOffset.AddHours) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerHour),
                nameof(DateTimeOffset.AddMinutes) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerMinute),
                nameof(DateTimeOffset.AddSeconds) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerSecond),
                nameof(DateTimeOffset.AddMilliseconds) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerMillisecond),
                nameof(DateTimeOffset.AddMicroseconds) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerMicrosecond),
                nameof(DateTimeOffset.AddTicks) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, 1),
                nameof(DateTimeOffset.Subtract) => new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex++,
                    $"{obj.Sql} - {arguments[0].Sql}",
                    CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!)
                ),
                _ => node
            };
        }
        else if (CheckConstantMethod<DateTimeOffset>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        return node.Method.Name switch
        {
            nameof(DateTimeOffset.Parse) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"@p{visitor.ParamIndex.Index++}",
                DateTimeOffset.Parse((string)arguments[0].Constant!).Ticks
            ),
            _ => node
        };
    }

    public Expression HandleTimeSpanMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null))
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(TimeSpan.Add) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, 1),
                nameof(TimeSpan.Subtract) => new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex++,
                    $"{obj.Sql} - {arguments[0].Sql}",
                    CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!)
                ),
                _ => node
            };
        }
        else if (CheckConstantMethod<TimeSpan>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        return node.Method.Name switch
        {
            nameof(TimeSpan.FromDays) => ResolveParse(node.Method, arguments, TimeSpan.TicksPerDay),
            nameof(TimeSpan.FromHours) => ResolveParse(node.Method, arguments, TimeSpan.TicksPerHour),
            nameof(TimeSpan.FromMinutes) => ResolveParse(node.Method, arguments, TimeSpan.TicksPerMinute),
            nameof(TimeSpan.FromSeconds) => ResolveParse(node.Method, arguments, TimeSpan.TicksPerSecond),
            nameof(TimeSpan.FromMilliseconds) => ResolveParse(node.Method, arguments, TimeSpan.TicksPerMillisecond),
            nameof(TimeSpan.FromMicroseconds) => ResolveParse(node.Method, arguments, TimeSpan.TicksPerMicrosecond),
            nameof(TimeSpan.FromTicks) => ResolveParse(node.Method, arguments, 1),
            _ => node
        };
    }

    public Expression HandleDateOnlyMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null))
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(DateOnly.AddYears) => ResolveRelativeDate(node.Method, obj.SQLExpression, arguments, "years"),
                nameof(DateOnly.AddMonths) => ResolveRelativeDate(node.Method, obj.SQLExpression, arguments, "months"),
                nameof(DateOnly.AddDays) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerDay),
                _ => node
            };
        }
        else if (CheckConstantMethod<DateOnly>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        return node;
    }

    public Expression HandleTimeOnlyMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null))
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(TimeOnly.Add) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, 1),
                nameof(TimeOnly.AddHours) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerHour),
                nameof(TimeOnly.AddMinutes) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerMinute),
                _ => node
            };
        }
        else if (CheckConstantMethod<TimeOnly>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        return node;
    }

    public Expression HandleGuidMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object == null)
        {
            if (arguments.Any(f => f.SQLExpression == null))
            {
                return Expression.Call(node.Method, arguments.Select(f => f.Expression));
            }

            switch (node.Method.Name)
            {
                case nameof(Guid.NewGuid):
                {
                    Guid guid = Guid.NewGuid();
                    string pName = $"@p{visitor.ParamIndex.Index++}";

                    return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex++, pName, guid);
                }
            }
        }
        else if (CheckConstantMethod<Guid>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        return node;
    }

    public Expression HandleQueryableMethod(MethodCallExpression node)
    {
        SQLTranslator translator = visitor.CloneDeeper(visitor.Level + 1);
        SQLQuery query = translator.Translate(node);

        if (node.Arguments.Count == 1)
        {
            if (node.Method.Name is nameof(Queryable.Any) or nameof(Queryable.All))
            {
                return new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex++,
                    $"EXISTS ({Environment.NewLine}{query.Sql}{Environment.NewLine})",
                    query.Parameters.Count != 0
                        ? query.Parameters.ToArray()
                        : null
                );
            }

            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"({Environment.NewLine}{query.Sql}{Environment.NewLine})",
                query.Parameters.Count != 0
                    ? query.Parameters.ToArray()
                    : null
            );
        }

        SQLExpression innerSql = new(
            node.Method.ReturnType,
            visitor.IdentifierIndex++,
            $"({Environment.NewLine}{query.Sql}{Environment.NewLine})",
            query.Parameters.Count != 0
                ? query.Parameters.ToArray()
                : null
        );

        List<ResolvedModel> arguments = node.Arguments
            .Skip(1)
            .Select(visitor.ResolveExpression)
            .ToList();

        if (arguments.Any(f => f.Sql == null))
        {
            return Expression.Call(node.Method, [innerSql, .. arguments.Select(f => f.Expression)]);
        }

        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters([innerSql, .. arguments.Select(f => f.SQLExpression!)]);

        return node.Method.Name switch
        {
            nameof(Queryable.Contains) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"{arguments[0].Sql} IN ({Environment.NewLine}{query.Sql}{Environment.NewLine})",
                parameters
            ),
            _ => node
        };
    }

    public Expression HandleEnumerableMethod(MethodCallExpression node, IEnumerable enumerable, List<ResolvedModel> arguments)
    {
        if (arguments.Any(f => f.Sql == null))
        {
            return Expression.Call(node.Object, node.Method, arguments.Select(f => f.Expression));
        }

        if (node.Object == null && CommonHelpers.IsSimple(node.Method.ReturnType))
        {
            object? result = node.Method.Invoke(null, [
                enumerable,
                ..node.Arguments.Skip(1).Select(CommonHelpers.GetConstantValue)
            ]);
            string pName = $"@p{visitor.ParamIndex.Index++}";

            return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex++, pName, result);
        }

        switch (node.Method.Name)
        {
            case nameof(Enumerable.Contains):
            {
                SQLiteParameter[] parameters = enumerable
                    .Cast<object>()
                    .Select(f => new SQLiteParameter
                    {
                        Name = $"@p{visitor.ParamIndex.Index++}",
                        Value = f
                    })
                    .ToArray();

                if (parameters.Length == 0)
                {
                    // TODO: Handle empty list
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex++,
                        $"{arguments[0].Sql} IN ()",
                        arguments[0].Parameters
                    );
                }

                return new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex++,
                    $"{arguments[0].Sql} IN ({string.Join(", ", parameters.Select(f => f.Name))})",
                    [.. arguments[0].Parameters ?? [], .. parameters]
                );
            }
        }

        return node;
    }

    public Expression HandleGroupingMethod(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case nameof(Enumerable.LongCount):
            case nameof(Enumerable.Count):
                return new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex++,
                    "COUNT(*)",
                    []
                );
        }

        SQLExpression? sqlExpression = null;

        // We have a selection like `g.Sum(f => f.Price)`, where `g` is a grouping.
        if (node.Arguments.Count == 2)
        {
            (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(node.Arguments[0]);

            Dictionary<string, Expression> newTableColumns = [];

            foreach (KeyValuePair<string, Expression> kvp in visitor.MethodArguments[pe])
            {
                if (kvp.Key.StartsWith(path))
                {
                    // +1 for the dot between the path and the key
                    string[] split = kvp.Key[(path.Length + nameof(IGrouping<,>.Key).Length + 1)..]
                        .Split('.', StringSplitOptions.RemoveEmptyEntries);

                    string newKey = string.Join('.', split);
                    newTableColumns[newKey] = kvp.Value;
                }
            }

            LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
            visitor.MethodArguments[lambda.Parameters[0]] = newTableColumns;
        }
        else
        {
            // If the path is empty, we are dealing with a grouping without a key, like `g.Sum()`.
            // We need to resolve the grouping key to the correct column.
            Expression expression = visitor.ResolveMember(node.Arguments[0]);

            if (expression is not SQLExpression expr)
            {
                throw new NotSupportedException("Grouping key could not be resolved.");
            }

            sqlExpression = expr;
        }

        return node.Method.Name switch
        {
            nameof(Enumerable.Sum) => AggregateExpression(node, "SUM", sqlExpression),
            nameof(Enumerable.Average) => AggregateExpression(node, "AVG", sqlExpression),
            nameof(Enumerable.Min) => AggregateExpression(node, "MIN", sqlExpression),
            nameof(Enumerable.Max) => AggregateExpression(node, "MAX", sqlExpression),
            _ => node
        };
    }

    private SQLExpression ResolveLike(MethodInfo method, SQLExpression obj, List<ResolvedModel> arguments, Func<object?, string> selectParameter, Func<SQLExpression, string> selectValue)
    {
        string rest = "ESCAPE '\\'";
        if (arguments.Count == 2)
        {
            StringComparison comparison = (StringComparison)arguments[1].Constant!;
            if (comparison is StringComparison.OrdinalIgnoreCase or StringComparison.CurrentCultureIgnoreCase or StringComparison.InvariantCultureIgnoreCase)
            {
                rest += " COLLATE NOCASE";
            }
        }

        if (arguments[0].IsConstant)
        {
            string pName = $"@p{visitor.ParamIndex.Index++}";
            SQLiteParameter parameter = new()
            {
                Name = pName,
                Value = arguments[0].Constant is string likeText
                    ? selectParameter(likeText
                        .Replace("\\", "\\\\")
                        .Replace("%", "\\%")
                        .Replace("_", "\\_"))
                    : arguments[0].Constant
            };

            SQLiteParameter[] parameters = obj.Parameters == null
                ? [parameter]
                : [.. obj.Parameters, parameter];

            return new SQLExpression(method.ReturnType, visitor.IdentifierIndex++, $"{obj.Sql} LIKE {pName} {rest}", parameters);
        }
        else
        {
            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj, arguments[0].SQLExpression!);

            return new SQLExpression(
                method.ReturnType,
                visitor.IdentifierIndex++,
                $"{obj.Sql} LIKE {selectValue(arguments[0].SQLExpression!)} {rest}",
                parameters
            );
        }
    }

    private Expression ResolveTrim(MethodCallExpression node, SQLExpression obj, List<ResolvedModel> arguments, string trimType)
    {
        if (arguments.Count == 0)
        {
            return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex++, $"{trimType}({obj.Sql})", obj.Parameters);
        }

        if (node.Arguments[0] is NewArrayExpression expression)
        {
            ResolvedModel[] args = expression.Expressions
                .Select(visitor.ResolveExpression)
                .ToArray();

            if (args.Any(f => f.Sql == null))
            {
                return Expression.Call(obj, node.Method, arguments.Select(f => f.Expression));
            }

            StringBuilder sb = new($"{trimType}({obj.Sql}, {args[0].Sql})");
            for (int i = 1; i < args.Length; i++)
            {
                sb.Insert(0, $"{trimType}(");
                sb.Append(", ");
                sb.Append(args[i].Sql);
                sb.Append(')');
            }

            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters([obj, .. args.Select(f => f.SQLExpression!)]);
            return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex++, sb.ToString(), parameters);
        }
        else
        {
            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj, arguments[0].SQLExpression!);

            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"{trimType}({obj.Sql}, {arguments[0].Sql})",
                parameters
            );
        }
    }

    private SQLExpression ResolveDateAdd(MethodInfo method, SQLExpression obj, List<ResolvedModel> arguments, long multiplyBy)
    {
        SQLiteParameter parameter = new()
        {
            Name = $"@p{visitor.ParamIndex.Index++}",
            Value = multiplyBy
        };

        return new SQLExpression(
            method.ReturnType,
            visitor.IdentifierIndex++,
            $"CAST({obj.Sql} + ({arguments[0].Sql} * {parameter.Name}) AS 'INTEGER')",
            [.. obj.Parameters ?? [], .. arguments[0].Parameters ?? [], parameter]
        );
    }

    private SQLExpression ResolveParse(MethodInfo method, List<ResolvedModel> arguments, long multiplyBy)
    {
        return new SQLExpression(
            method.ReturnType,
            visitor.IdentifierIndex++,
            $"CAST({multiplyBy} * {arguments[0].Sql} AS INTEGER)",
            arguments[0].Parameters
        );
    }

    private SQLExpression ResolveRelativeDate(MethodInfo method, SQLExpression obj, List<ResolvedModel> arguments, string addType)
    {
        (SQLiteParameter tickParameter, SQLiteParameter tickToSecondParameter) = CreateHelperDateParameters();

        if (arguments[0].IsConstant)
        {
            SQLiteParameter parameter = new()
            {
                Name = $"@p{visitor.ParamIndex.Index++}",
                Value = $"+{arguments[0].Constant} {addType}"
            };

            SQLiteParameter[] parameters = [.. obj.Parameters ?? [], tickParameter, tickToSecondParameter, parameter];

            string sql = $"CAST(STRFTIME('%s',DATETIME(({obj.Sql} - {tickParameter.Name}) / {tickToSecondParameter.Name}, 'unixepoch', {parameter.Name})) AS INTEGER) * {tickToSecondParameter.Name} + {tickParameter.Name}";

            return new SQLExpression(method.ReturnType, visitor.IdentifierIndex++, sql, parameters);
        }
        else
        {
            SQLiteParameter[] parameters = [.. obj.Parameters ?? [], .. arguments[0].Parameters ?? [], tickParameter, tickToSecondParameter];

            string sql = $"CAST(STRFTIME('%s',DATETIME(({obj.Sql} - {tickParameter.Name}) / {tickToSecondParameter.Name}, 'unixepoch', '+'||{arguments[0].Sql}||' {addType}')) AS INTEGER) * {tickToSecondParameter.Name} + {tickParameter.Name}";

            return new SQLExpression(
                method.ReturnType,
                visitor.IdentifierIndex++,
                sql,
                parameters
            );
        }
    }

    private (SQLiteParameter TickParameter, SQLiteParameter TickToSecondParameter) CreateHelperDateParameters()
    {
        SQLiteParameter tickParameter = new()
        {
            Name = $"@p{visitor.ParamIndex.Index++}",
            Value = 621355968000000000 // new DateTime(1970, 1, 1).Ticks
        };
        SQLiteParameter tickToSecondParameter = new()
        {
            Name = $"@p{visitor.ParamIndex.Index++}",
            Value = TimeSpan.TicksPerSecond
        };

        return (tickParameter, tickToSecondParameter);
    }

    private SQLExpression AggregateExpression(MethodCallExpression node, string aggregateFunction, SQLExpression? sqlExpression)
    {
        if (node.Arguments.Count == 1)
        {
            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"{aggregateFunction}({sqlExpression!.Sql})",
                sqlExpression.Parameters
            );
        }
        else
        {
            LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
            Expression resolvedExpression = visitor.Visit(lambda.Body);
            if (resolvedExpression is not SQLExpression sql)
            {
                throw new NotSupportedException("Sum could not resolve the expression.");
            }

            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"{aggregateFunction}({sql.Sql})",
                sql.Parameters
            );
        }
    }

    private bool CheckConstantMethod<T>(MethodCallExpression node, List<ResolvedModel> arguments, [MaybeNullWhen(false)] out Expression expression)
    {
        if (arguments.Any(f => f.Sql == null))
        {
            expression = Expression.Call(node.Method, arguments.Select(f => f.Expression));
            return true;
        }

        Type type = typeof(T);

        if (node.Method.ReturnType.IsAssignableTo(type) && arguments.All(f => f.IsConstant))
        {
            object? result = node.Method.Invoke(null, arguments.Select(f => f.Constant).ToArray());

            if (result is not T)
            {
                throw new Exception($"Expected {type}, somehow got {result?.GetType()}");
            }

            string pName = $"@p{visitor.ParamIndex.Index++}";
            expression = new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex++, pName, result);
            return true;
        }

        expression = null;
        return false;
    }
}