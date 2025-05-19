using System.Collections;
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
        if (node.Object != null)
        {
            (SQLExpression? obj, Expression objectExpression) = visitor.ResolveExpression(node.Object);
            List<(bool IsConstant, object? Constant, SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
                .Select(visitor.ResolveExpressionWithConstant)
                .ToList();

            if (obj == null || arguments.Any(f => f.Sql == null))
            {
                return Expression.Call(objectExpression, node.Method, arguments.Select(f => f.Expression));
            }

            switch (node.Method.Name)
            {
                case nameof(string.Contains):
                {
                    return ResolveLike(node.Method, obj, arguments!, value => $"%{value}%", valueSql => $"'%'||{valueSql}||'%'");
                }
                case nameof(string.StartsWith):
                {
                    return ResolveLike(node.Method, obj, arguments!, value => $"{value}%", valueSql => $"{valueSql}||'%'");
                }
                case nameof(string.EndsWith):
                {
                    return ResolveLike(node.Method, obj, arguments!, value => $"%{value}", valueSql => $"'%'||{valueSql}");
                }
                case nameof(string.Equals):
                {
                    SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj, arguments[0].Sql!);
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex++,
                        $"{obj.Sql} = {arguments[0].Sql!.Sql}",
                        parameters
                    );
                }
                case nameof(string.IndexOf):
                {
                    SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj, arguments[0].Sql!);
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex++,
                        $"INSTR({obj.Sql}, {arguments[0].Sql!.Sql})",
                        parameters
                    );
                }
                case nameof(string.Replace):
                {
                    SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj, arguments[0].Sql!, arguments[1].Sql!);
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex++,
                        $"REPLACE({obj.Sql}, {arguments[0].Sql!.Sql}, {arguments[1].Sql!.Sql})",
                        parameters
                    );
                }
                case nameof(string.Trim):
                {
                    return ResolveTrim(node, obj, arguments!, "TRIM");
                }
                case nameof(string.TrimStart):
                {
                    return ResolveTrim(node, obj, arguments!, "LTRIM");
                }
                case nameof(string.TrimEnd):
                {
                    return ResolveTrim(node, obj, arguments!, "RTRIM");
                }
                case nameof(string.Substring):
                {
                    if (node.Arguments.Count == 2)
                    {
                        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj, arguments[0].Sql!, arguments[1].Sql!);
                        return new SQLExpression(
                            node.Method.ReturnType,
                            visitor.IdentifierIndex++,
                            $"SUBSTR({obj.Sql}, {arguments[0].Sql!.Sql}, {arguments[1].Sql!.Sql})",
                            parameters
                        );
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj, arguments[0].Sql!);
                        return new SQLExpression(
                            node.Method.ReturnType,
                            visitor.IdentifierIndex++,
                            $"SUBSTR({obj.Sql}, {arguments[0].Sql!.Sql})",
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
        else
        {
            List<(bool IsConstant, object? Constant, SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
                .Select(visitor.ResolveExpressionWithConstant)
                .ToList();

            if (arguments.Any(f => f.Sql == null))
            {
                return Expression.Call(node.Method, arguments.Select(f => f.Expression));
            }

            switch (node.Method.Name)
            {
                case nameof(string.IsNullOrEmpty):
                {
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex++,
                        $"({arguments[0].Sql!.Sql} IS NULL OR {arguments[0].Sql!.Sql} = '')",
                        arguments[0].Sql!.Parameters
                    );
                }
                case nameof(string.IsNullOrWhiteSpace):
                {
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex++,
                        $"({arguments[0].Sql!.Sql} IS NULL OR TRIM({arguments[0].Sql!.Sql}, ' ') = '')",
                        arguments[0].Sql!.Parameters
                    );
                }
                // TODO: Add concat and join
            }
        }

        return node;
    }

    public Expression HandleMathMethod(MethodCallExpression node)
    {
        List<(SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (arguments.Any(f => f.Sql == null))
        {
            return Expression.Call(node.Method, arguments.Select(f => f.Expression));
        }

        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(arguments.Select(f => f.Sql!).ToArray());

        return node.Method.Name switch
        {
            nameof(Math.Min) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"(CASE WHEN {arguments[0].Sql!.Sql} < {arguments[1].Sql!.Sql} THEN {arguments[0].Sql!.Sql} ELSE {arguments[1].Sql!.Sql} END)",
                parameters
            ),
            nameof(Math.Max) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"(CASE WHEN {arguments[0].Sql!.Sql} > {arguments[1].Sql!.Sql} THEN {arguments[0].Sql!.Sql} ELSE {arguments[1].Sql!.Sql} END)",
                parameters
            ),
            nameof(Math.Abs) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"ABS({arguments[0].Sql!.Sql})",
                parameters
            ),
            nameof(Math.Round) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"ROUND({arguments[0].Sql!.Sql})",
                parameters
            ),
            nameof(Math.Ceiling) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"CEIL({arguments[0].Sql!.Sql})",
                parameters
            ),
            nameof(Math.Floor) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"FLOOR({arguments[0].Sql!.Sql})",
                parameters
            ),
            _ => node
        };
    }

    public Expression HandleDateTimeMethod(MethodCallExpression node)
    {
        if (node.Object != null)
        {
            (SQLExpression? obj, Expression objectExpression) = visitor.ResolveExpression(node.Object);
            List<(bool IsConstant, object? Constant, SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
                .Select(visitor.ResolveExpressionWithConstant)
                .ToList();

            if (obj == null || arguments.Any(f => f.Sql == null))
            {
                return Expression.Call(objectExpression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(DateTime.Add) => ResolveDateAdd(node.Method, obj, arguments!, 1),
                nameof(DateTime.AddYears) => ResolveRelativeDate(node.Method, obj, arguments!, "years"),
                nameof(DateTime.AddMonths) => ResolveRelativeDate(node.Method, obj, arguments!, "months"),
                nameof(DateTime.AddDays) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerDay),
                nameof(DateTime.AddHours) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerHour),
                nameof(DateTime.AddMinutes) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerMinute),
                nameof(DateTime.AddSeconds) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerSecond),
                nameof(DateTime.AddMilliseconds) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerMillisecond),
                nameof(DateTime.AddMicroseconds) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerMicrosecond),
                nameof(DateTime.AddTicks) => ResolveDateAdd(node.Method, obj, arguments!, 1),
                nameof(DateTime.Subtract) => new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex++,
                    $"{obj.Sql} - {arguments[0].Sql}",
                    CommonHelpers.CombineParameters(obj, arguments[0].Sql!)
                ),
                _ => node
            };
        }
        else
        {
            List<(bool IsConstant, object? Constant, SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
                .Select(visitor.ResolveExpressionWithConstant)
                .ToList();

            if (arguments.Any(f => f.Sql == null))
            {
                return Expression.Call(node.Method, arguments.Select(f => f.Expression));
            }

            switch (node.Method.Name)
            {
                case nameof(DateTime.Parse):
                {
                    string pName = $"@p{visitor.ParamIndex.Index++}";
                    return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex++, pName, DateTime.Parse((string)arguments[0].Constant!).Ticks);
                }
                case nameof(DateTime.FromBinary):
                {
                    string pName = $"@p{visitor.ParamIndex.Index++}";
                    return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex++, pName, DateTime.FromBinary((long)arguments[0].Constant!).Ticks);
                }
            }
        }

        return node;
    }

    public Expression HandleDateTimeOffsetMethod(MethodCallExpression node)
    {
        if (node.Object != null)
        {
            (SQLExpression? obj, Expression objectExpression) = visitor.ResolveExpression(node.Object);
            List<(bool IsConstant, object? Constant, SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
                .Select(visitor.ResolveExpressionWithConstant)
                .ToList();

            if (obj == null || arguments.Any(f => f.Sql == null))
            {
                return Expression.Call(objectExpression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(DateTimeOffset.Add) => ResolveDateAdd(node.Method, obj, arguments!, 1),
                nameof(DateTimeOffset.AddYears) => ResolveRelativeDate(node.Method, obj, arguments!, "years"),
                nameof(DateTimeOffset.AddMonths) => ResolveRelativeDate(node.Method, obj, arguments!, "months"),
                nameof(DateTimeOffset.AddDays) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerDay),
                nameof(DateTimeOffset.AddHours) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerHour),
                nameof(DateTimeOffset.AddMinutes) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerMinute),
                nameof(DateTimeOffset.AddSeconds) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerSecond),
                nameof(DateTimeOffset.AddMilliseconds) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerMillisecond),
                nameof(DateTimeOffset.AddMicroseconds) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerMicrosecond),
                nameof(DateTimeOffset.AddTicks) => ResolveDateAdd(node.Method, obj, arguments!, 1),
                nameof(DateTimeOffset.Subtract) => new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex++,
                    $"{obj.Sql} - {arguments[0].Sql}",
                    CommonHelpers.CombineParameters(obj, arguments[0].Sql!)
                ),
                _ => node
            };
        }
        else
        {
            List<(bool IsConstant, object? Constant, SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
                .Select(visitor.ResolveExpressionWithConstant)
                .ToList();

            if (arguments.Any(f => f.Sql == null))
            {
                return Expression.Call(node.Method, arguments.Select(f => f.Expression));
            }

            switch (node.Method.Name)
            {
                case nameof(DateTimeOffset.Parse):
                {
                    string pName = $"@p{visitor.ParamIndex.Index++}";
                    return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex++, pName, DateTimeOffset.Parse((string)arguments[0].Constant!).Ticks);
                }
            }
        }

        return node;
    }

    public Expression HandleTimeSpanMethod(MethodCallExpression node)
    {
        if (node.Object != null)
        {
            (SQLExpression? obj, Expression objectExpression) = visitor.ResolveExpression(node.Object);
            List<(bool IsConstant, object? Constant, SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
                .Select(visitor.ResolveExpressionWithConstant)
                .ToList();

            if (obj == null || arguments.Any(f => f.Sql == null))
            {
                return Expression.Call(objectExpression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(TimeSpan.Add) => ResolveDateAdd(node.Method, obj, arguments!, 1),
                nameof(TimeSpan.Subtract) => new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex++,
                    $"{obj.Sql} - {arguments[0].Sql}",
                    CommonHelpers.CombineParameters(obj, arguments[0].Sql!)
                ),
                _ => node
            };
        }

        return node;
    }

    public Expression HandleDateOnlyMethod(MethodCallExpression node)
    {
        if (node.Object != null)
        {
            (SQLExpression? obj, Expression objectExpression) = visitor.ResolveExpression(node.Object);
            List<(bool IsConstant, object? Constant, SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
                .Select(visitor.ResolveExpressionWithConstant)
                .ToList();

            if (obj == null || arguments.Any(f => f.Sql == null))
            {
                return Expression.Call(objectExpression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(DateOnly.AddYears) => ResolveRelativeDate(node.Method, obj, arguments!, "years"),
                nameof(DateOnly.AddMonths) => ResolveRelativeDate(node.Method, obj, arguments!, "months"),
                nameof(DateOnly.AddDays) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerDay),
                _ => node
            };
        }

        return node;
    }

    public Expression HandleTimeOnlyMethod(MethodCallExpression node)
    {
        if (node.Object != null)
        {
            (SQLExpression? obj, Expression objectExpression) = visitor.ResolveExpression(node.Object);
            List<(bool IsConstant, object? Constant, SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
                .Select(visitor.ResolveExpressionWithConstant)
                .ToList();

            if (obj == null || arguments.Any(f => f.Sql == null))
            {
                return Expression.Call(objectExpression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(TimeOnly.Add) => ResolveDateAdd(node.Method, obj, arguments!, 1),
                nameof(TimeOnly.AddHours) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerHour),
                nameof(TimeOnly.AddMinutes) => ResolveDateAdd(node.Method, obj, arguments!, TimeSpan.TicksPerMinute),
                _ => node
            };
        }

        return node;
    }

    public Expression HandleGuidMethod(MethodCallExpression node)
    {
        if (node.Object == null)
        {
            List<(bool IsConstant, object? Constant, SQLExpression? SqlExpression, Expression Expression)> arguments = node.Arguments
                .Select(visitor.ResolveExpressionWithConstant)
                .ToList();

            if (arguments.Any(f => f.SqlExpression == null))
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

        List<(SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
            .Skip(1)
            .Select(visitor.ResolveExpression)
            .ToList();

        if (arguments.Any(f => f.Sql == null))
        {
            return Expression.Call(node.Method, [innerSql, .. arguments.Select(f => f.Expression)]);
        }

        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters([innerSql, .. arguments.Select(f => f.Sql!)]);

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

    public Expression HandleEnumerableMethod(MethodCallExpression node, IEnumerable enumerable, List<(bool IsConstant, object? Constant, SQLExpression? Sql, Expression Expression)> arguments)
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
                        $"{arguments[0].Sql!.Sql} IN ()",
                        arguments[0].Sql!.Parameters
                    );
                }

                return new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex++,
                    $"{arguments[0].Sql!.Sql} IN ({string.Join(", ", parameters.Select(f => f.Name))})",
                    [.. arguments[0].Sql!.Parameters ?? [], .. parameters]
                );
            }
        }

        return node;
    }

    private SQLExpression ResolveLike(MethodInfo method, SQLExpression obj, List<(bool IsConstant, object? Constant, SQLExpression Sql, Expression Expression)> arguments, Func<object?, string> selectParameter, Func<SQLExpression, string> selectValue)
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
            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj, arguments[0].Sql);

            return new SQLExpression(
                method.ReturnType,
                visitor.IdentifierIndex++,
                $"{obj.Sql} LIKE {selectValue(arguments[0].Sql)} {rest}",
                parameters
            );
        }
    }

    private Expression ResolveTrim(MethodCallExpression node, SQLExpression obj, List<(bool IsConstant, object? Constant, SQLExpression Sql, Expression Expression)> arguments, string trimType)
    {
        if (arguments.Count == 0)
        {
            return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex++, $"{trimType}({obj.Sql})", obj.Parameters);
        }

        if (node.Arguments[0] is NewArrayExpression expression)
        {
            (SQLExpression? Sql, Expression Expression)[] args = expression.Expressions
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

            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters([obj, .. args.Select(f => f.Sql!)]);
            return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex++, sb.ToString(), parameters);
        }
        else
        {
            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj, arguments[0].Sql);

            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex++,
                $"{trimType}({obj.Sql}, {arguments[0].Sql.Sql})",
                parameters
            );
        }
    }

    private SQLExpression ResolveDateAdd(MethodInfo method, SQLExpression obj, List<(bool IsConstant, object? Constant, SQLExpression Sql, Expression Expression)> arguments, long multiplyBy)
    {
        SQLiteParameter parameter = new()
        {
            Name = $"@p{visitor.ParamIndex.Index++}",
            Value = multiplyBy
        };

        return new SQLExpression(
            method.ReturnType,
            visitor.IdentifierIndex++,
            $"CAST({obj.Sql} + ({arguments[0].Sql.Sql} * {parameter.Name}) AS 'INTEGER')",
            [.. obj.Parameters ?? [], .. arguments[0].Sql.Parameters ?? [], parameter]
        );
    }

    private SQLExpression ResolveRelativeDate(MethodInfo method, SQLExpression obj, List<(bool IsConstant, object? Constant, SQLExpression Sql, Expression Expression)> arguments, string addType)
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
            SQLiteParameter[] parameters = [.. obj.Parameters ?? [], .. arguments[0].Sql.Parameters ?? [], tickParameter, tickToSecondParameter];

            string sql = $"CAST(STRFTIME('%s',DATETIME(({obj.Sql} - {tickParameter.Name}) / {tickToSecondParameter.Name}, 'unixepoch', '+'||{arguments[0].Sql.Sql}||' {addType}')) AS INTEGER) * {tickToSecondParameter.Name} + {tickParameter.Name}";

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
}