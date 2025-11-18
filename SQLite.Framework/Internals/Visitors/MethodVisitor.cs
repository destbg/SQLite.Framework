using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
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

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null) || node.Method.Name == nameof(string.ToString))
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
                case nameof(string.IndexOf):
                {
                    SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!);
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"INSTR({obj.Sql}, {arguments[0].Sql}) - 1",
                        parameters
                    );
                }
                case nameof(string.LastIndexOf):
                {
                    SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!);
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"CASE WHEN INSTR({obj.Sql}, {arguments[0].Sql}) = 0 THEN -1 ELSE LENGTH({obj.Sql}) - INSTR(REPLACE(REPLACE({obj.Sql}, REPLACE({arguments[0].Sql}, '%', '\\%'), '<<<>>>'), '<<<>>>', ''), '<<<>>>') - LENGTH(REPLACE({arguments[0].Sql}, '%', '\\%')) END",
                        parameters
                    );
                }
                case nameof(string.Insert):
                {
                    SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!, arguments[1].SQLExpression!);
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"SUBSTR({obj.Sql}, 1, {arguments[0].Sql}) || {arguments[1].Sql} || SUBSTR({obj.Sql}, {arguments[0].Sql} + 1)",
                        parameters
                    );
                }
                case nameof(string.Remove):
                {
                    if (node.Arguments.Count == 2)
                    {
                        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!, arguments[1].SQLExpression!);
                        return new SQLExpression(
                            node.Method.ReturnType,
                            visitor.IdentifierIndex.Index++,
                            $"SUBSTR({obj.Sql}, 1, {arguments[0].Sql}) || SUBSTR({obj.Sql}, {arguments[0].Sql} + {arguments[1].Sql} + 1)",
                            parameters
                        );
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!);
                        return new SQLExpression(
                            node.Method.ReturnType,
                            visitor.IdentifierIndex.Index++,
                            $"SUBSTR({obj.Sql}, 1, {arguments[0].Sql})",
                            parameters
                        );
                    }
                }
                case nameof(string.Replace):
                {
                    SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!, arguments[1].SQLExpression!);
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
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
                            visitor.IdentifierIndex.Index++,
                            $"SUBSTR({obj.Sql}, {arguments[0].Sql} + 1, {arguments[1].Sql})",
                            parameters
                        );
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!);
                        return new SQLExpression(
                            node.Method.ReturnType,
                            visitor.IdentifierIndex.Index++,
                            $"SUBSTR({obj.Sql}, {arguments[0].Sql} + 1)",
                            parameters
                        );
                    }
                }
                case nameof(string.ToUpper):
                case nameof(string.ToUpperInvariant):
                {
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"UPPER({obj.Sql})",
                        obj.Parameters
                    );
                }
                case nameof(string.ToLower):
                case nameof(string.ToLowerInvariant):
                {
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"LOWER({obj.Sql})",
                        obj.Parameters
                    );
                }
                case nameof(string.PadLeft):
                {
                    if (node.Arguments.Count == 1)
                    {
                        SQLiteParameter spaceParam = new()
                        {
                            Name = $"@p{visitor.ParamIndex.Index++}",
                            Value = ' '
                        };
                        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!);
                        parameters = [.. parameters ?? [], spaceParam];

                        return new SQLExpression(
                            node.Method.ReturnType,
                            visitor.IdentifierIndex.Index++,
                            $"(CASE WHEN LENGTH({obj.Sql}) >= {arguments[0].Sql} THEN {obj.Sql} ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(({arguments[0].Sql} - LENGTH({obj.Sql})) / 2 + ({arguments[0].Sql} - LENGTH({obj.Sql})) % 2)), '00', {spaceParam.Name}), 1, {arguments[0].Sql} - LENGTH({obj.Sql})) || {obj.Sql}) END)",
                            parameters
                        );
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!, arguments[1].SQLExpression!);

                        return new SQLExpression(
                            node.Method.ReturnType,
                            visitor.IdentifierIndex.Index++,
                            $"(CASE WHEN LENGTH({obj.Sql}) >= {arguments[0].Sql} THEN {obj.Sql} ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB({arguments[0].Sql} - LENGTH({obj.Sql}))), '00', {arguments[1].Sql}), 1, {arguments[0].Sql} - LENGTH({obj.Sql})) || {obj.Sql}) END)",
                            parameters
                        );
                    }
                }
                case nameof(string.PadRight):
                {
                    if (node.Arguments.Count == 1)
                    {
                        SQLiteParameter spaceParam = new()
                        {
                            Name = $"@p{visitor.ParamIndex.Index++}",
                            Value = ' '
                        };
                        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!);
                        parameters = [.. parameters ?? [], spaceParam];

                        return new SQLExpression(
                            node.Method.ReturnType,
                            visitor.IdentifierIndex.Index++,
                            $"(CASE WHEN LENGTH({obj.Sql}) >= {arguments[0].Sql} THEN {obj.Sql} ELSE ({obj.Sql} || (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(({arguments[0].Sql} - LENGTH({obj.Sql})) / 2 + ({arguments[0].Sql} - LENGTH({obj.Sql})) % 2)), '00', {spaceParam.Name}), 1, {arguments[0].Sql} - LENGTH({obj.Sql})))) END)",
                            parameters
                        );
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!, arguments[1].SQLExpression!);

                        return new SQLExpression(
                            node.Method.ReturnType,
                            visitor.IdentifierIndex.Index++,
                            $"(CASE WHEN LENGTH({obj.Sql}) >= {arguments[0].Sql} THEN {obj.Sql} ELSE ({obj.Sql} || (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB({arguments[0].Sql} - LENGTH({obj.Sql}))), '00', {arguments[1].Sql}), 1, {arguments[0].Sql} - LENGTH({obj.Sql})))) END)",
                            parameters
                        );
                    }
                }
            }
        }
        else if (CheckConstantMethod<string>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        switch (node.Method.Name)
        {
            case nameof(string.IsNullOrEmpty):
                return new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex.Index++,
                    $"({arguments[0].Sql} IS NULL OR {arguments[0].Sql} = '')",
                    arguments[0].Parameters
                );
            case nameof(string.IsNullOrWhiteSpace):
                return new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex.Index++,
                    $"({arguments[0].Sql} IS NULL OR TRIM({arguments[0].Sql}, ' ') = '')",
                    arguments[0].Parameters
                );
            case nameof(string.Concat):
                return new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex.Index++,
                    string.Join(" || ", arguments.Select(f => f.Sql)),
                    CommonHelpers.CombineParameters(arguments.Select(f => f.SQLExpression!).ToArray())
                );
            case nameof(string.Join):
                if (node.Arguments[1] is NewArrayExpression arrayExpr)
                {
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        string.Join($" || {arguments[0].Sql} || ", arrayExpr.Expressions.Select(e => visitor.ResolveExpression(e).Sql)),
                        CommonHelpers.CombineParameters([arguments[0].SQLExpression!, .. arrayExpr.Expressions.Select(e => visitor.ResolveExpression(e).SQLExpression!)])
                    );
                }

                return node;
            case nameof(string.Compare):
                return new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex.Index++,
                    $"(CASE WHEN {arguments[0].Sql} = {arguments[1].Sql} THEN 0 WHEN {arguments[0].Sql} < {arguments[1].Sql} THEN -1 ELSE 1 END)",
                    CommonHelpers.CombineParameters(arguments[0].SQLExpression!, arguments[1].SQLExpression!)
                );
            case nameof(string.Equals):
                if (arguments.Count == 3 && arguments[2].Constant is StringComparison comparison)
                {
                    string collation = comparison switch
                    {
                        StringComparison.OrdinalIgnoreCase or
                        StringComparison.CurrentCultureIgnoreCase or
                        StringComparison.InvariantCultureIgnoreCase => " COLLATE NOCASE",
                        _ => ""
                    };

                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"({arguments[0].Sql} = {arguments[1].Sql}{collation})",
                        CommonHelpers.CombineParameters(arguments[0].SQLExpression!, arguments[1].SQLExpression!)
                    );
                }

                return new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex.Index++,
                    $"({arguments[0].Sql} = {arguments[1].Sql})",
                    CommonHelpers.CombineParameters(arguments[0].SQLExpression!, arguments[1].SQLExpression!)
                );
            default:
                return node;
        }
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
                visitor.IdentifierIndex.Index++,
                $"(CASE WHEN {arguments[0].Sql} < {arguments[1].Sql} THEN {arguments[0].Sql} ELSE {arguments[1].Sql} END)",
                parameters
            ),
            nameof(Math.Max) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"(CASE WHEN {arguments[0].Sql} > {arguments[1].Sql} THEN {arguments[0].Sql} ELSE {arguments[1].Sql} END)",
                parameters
            ),
            nameof(Math.Abs) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"ABS({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Round) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                arguments.Count == 2 ? $"ROUND({arguments[0].Sql}, {arguments[1].Sql})" : $"ROUND({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Ceiling) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"CEIL({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Floor) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"FLOOR({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Pow) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"POWER({arguments[0].Sql}, {arguments[1].Sql})",
                parameters
            ),
            nameof(Math.Sign) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"(CASE WHEN {arguments[0].Sql} > 0 THEN 1 WHEN {arguments[0].Sql} < 0 THEN -1 ELSE 0 END)",
                parameters
            ),
            nameof(Math.Sqrt) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"SQRT({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Exp) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"EXP({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Log) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                arguments.Count == 2 ? $"(LOG({arguments[0].Sql}) / LOG({arguments[1].Sql}))" : $"LOG({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Log10) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"LOG10({arguments[0].Sql})",
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

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null) || node.Method.Name == nameof(DateTime.ToString))
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
                _ => node
            };
        }

        if (CheckConstantMethod<DateTime>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        return node;
    }

    public Expression HandleDateTimeOffsetMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null) || node.Method.Name == nameof(DateTimeOffset.ToString))
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
                _ => node
            };
        }

        if (CheckConstantMethod<DateTimeOffset>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        return node;
    }

    public Expression HandleTimeSpanMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null) || node.Method.Name == nameof(TimeSpan.ToString))
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(TimeSpan.Add) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, 1),
                nameof(TimeSpan.Subtract) => new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex.Index++,
                    $"{obj.Sql} - {arguments[0].Sql}",
                    CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!)
                ),
                nameof(TimeSpan.Negate) => new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex.Index++,
                    $"(-{obj.Sql})",
                    obj.Parameters
                ),
                nameof(TimeSpan.Duration) => new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex.Index++,
                    $"ABS({obj.Sql})",
                    obj.Parameters
                ),
                _ => node
            };
        }

        if (CheckConstantMethod<TimeSpan>(node, arguments, out Expression? expression))
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

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null) || node.Method.Name == nameof(DateOnly.ToString))
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

        if (CheckConstantMethod<DateOnly>(node, arguments, out Expression? expression))
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

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null) || node.Method.Name == nameof(TimeOnly.ToString))
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

        if (CheckConstantMethod<TimeOnly>(node, arguments, out Expression? expression))
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
            if (arguments.Any(f => f.SQLExpression == null) || node.Method.Name == nameof(Guid.ToString))
            {
                return Expression.Call(node.Method, arguments.Select(f => f.Expression));
            }

            switch (node.Method.Name)
            {
                case nameof(Guid.NewGuid):
                {
                    Guid guid = Guid.NewGuid();
                    string pName = $"@p{visitor.ParamIndex.Index++}";

                    return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex.Index++, pName, guid);
                }
            }
        }

        if (CheckConstantMethod<Guid>(node, arguments, out Expression? expression))
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
                    visitor.IdentifierIndex.Index++,
                    $"EXISTS ({Environment.NewLine}{query.Sql}{Environment.NewLine})",
                    query.Parameters.Count != 0
                        ? query.Parameters.ToArray()
                        : null
                );
            }

            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"({Environment.NewLine}{query.Sql}{Environment.NewLine})",
                query.Parameters.Count != 0
                    ? query.Parameters.ToArray()
                    : null
            );
        }

        SQLExpression innerSql = new(
            node.Method.ReturnType,
            visitor.IdentifierIndex.Index++,
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
                visitor.IdentifierIndex.Index++,
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

            return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex.Index++, pName, result);
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
                    // For an empty list, `IN ()` is invalid SQL and should always return false.
                    // We use `0 = 1` to ensure the condition is never true.
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        "0 = 1",
                        arguments[0].Parameters
                    );
                }

                return new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex.Index++,
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
                    visitor.IdentifierIndex.Index++,
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
                    int length = path.Length + nameof(IGrouping<,>.Key).Length + 1;
                    string[] split = kvp.Key[Math.Min(length, kvp.Key.Length)..]
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

    public Expression HandleEnumMethod(MethodCallExpression node)
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
                case nameof(Enum.HasFlag):
                {
                    SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, arguments[0].SQLExpression!);
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"(({obj.Sql} & {arguments[0].Sql}) = {arguments[0].Sql})",
                        parameters
                    );
                }
                case nameof(Enum.ToString):
                {
                    Type enumType = node.Object!.Type;
                    Array enumValuesArray = Enum.GetValuesAsUnderlyingType(enumType);
                    string[] enumNames = Enum.GetNames(enumType);

                    List<string> caseClauses = new();
                    List<SQLiteParameter> nameParams = new();

                    for (int i = 0; i < enumValuesArray.Length; i++)
                    {
                        object enumValue = enumValuesArray.GetValue(i)!;
                        long numericValue = Convert.ToInt64(enumValue);
                        string enumName = enumNames[i];

                        SQLiteParameter nameParam = new()
                        {
                            Name = $"@p{visitor.ParamIndex.Index++}",
                            Value = enumName
                        };
                        nameParams.Add(nameParam);
                        caseClauses.Add($"WHEN {numericValue} THEN {nameParam.Name}");
                    }

                    string caseExpression = $"(CASE {obj.Sql} {string.Join(" ", caseClauses)} ELSE CAST({obj.Sql} AS TEXT) END)";

                    SQLiteParameter[]? parameters = obj.Parameters == null
                        ? [.. nameParams]
                        : [.. obj.Parameters, .. nameParams];

                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        caseExpression,
                        parameters
                    );
                }
            }
        }

        if (CheckConstantMethod<Enum>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        // Handle Enum.Parse<T>(string) or Enum.Parse(Type, string)
        if (node.Method.Name == nameof(Enum.Parse))
        {
            Type enumType;
            ResolvedModel stringArg;

            if (node.Method.IsGenericMethod)
            {
                // Enum.Parse<T>(string)
                enumType = node.Method.GetGenericArguments()[0];
                stringArg = arguments[0];
            }
            else
            {
                // Enum.Parse(Type, string) - Type is first argument
                if (arguments[0].IsConstant && arguments[0].Constant is Type type)
                {
                    enumType = type;
                    stringArg = arguments[1];
                }
                else
                {
                    return node;
                }
            }

            if (stringArg.SQLExpression == null)
            {
                return node;
            }

            Array enumValuesArray = Enum.GetValuesAsUnderlyingType(enumType);
            string[] enumNames = Enum.GetNames(enumType);

            List<string> caseClauses = new();
            List<SQLiteParameter> parameters = new();

            for (int i = 0; i < enumValuesArray.Length; i++)
            {
                object enumValue = enumValuesArray.GetValue(i)!;
                long numericValue = Convert.ToInt64(enumValue);
                string enumName = enumNames[i];

                SQLiteParameter nameParam = new()
                {
                    Name = $"@p{visitor.ParamIndex.Index++}",
                    Value = enumName
                };
                parameters.Add(nameParam);
                caseClauses.Add($"WHEN {nameParam.Name} THEN {numericValue}");
            }

            string caseExpression = $"(CASE {stringArg.Sql} {string.Join(" ", caseClauses)} ELSE NULL END)";

            SQLiteParameter[]? allParams = stringArg.Parameters == null
                ? [.. parameters]
                : [.. stringArg.Parameters, .. parameters];

            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                caseExpression,
                allParams
            );
        }

        return node;
    }

    public Expression HandleCharMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (arguments.Count > 0 && arguments[0].SQLExpression != null)
        {
            switch (node.Method.Name)
            {
                case nameof(char.ToLower):
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"LOWER({arguments[0].Sql})",
                        arguments[0].Parameters
                    );
                case nameof(char.ToUpper):
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"UPPER({arguments[0].Sql})",
                        arguments[0].Parameters
                    );
                case nameof(char.IsWhiteSpace):
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"TRIM({arguments[0].Sql}) = ''",
                        arguments[0].Parameters
                    );
                case nameof(char.IsAsciiDigit):
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"({arguments[0].Sql} >= '0' AND {arguments[0].Sql} <= '9')",
                        arguments[0].Parameters
                    );
                case nameof(char.IsAsciiLetter):
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"(({arguments[0].Sql} >= 'a' AND {arguments[0].Sql} <= 'z') OR ({arguments[0].Sql} >= 'A' AND {arguments[0].Sql} <= 'Z'))",
                        arguments[0].Parameters
                    );
                case nameof(char.IsAsciiLetterOrDigit):
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"(({arguments[0].Sql} >= '0' AND {arguments[0].Sql} <= '9') OR ({arguments[0].Sql} >= 'a' AND {arguments[0].Sql} <= 'z') OR ({arguments[0].Sql} >= 'A' AND {arguments[0].Sql} <= 'Z'))",
                        arguments[0].Parameters
                    );
                case nameof(char.IsAsciiLetterLower):
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"({arguments[0].Sql} >= 'a' AND {arguments[0].Sql} <= 'z')",
                        arguments[0].Parameters
                    );
                case nameof(char.IsAsciiLetterUpper):
                    return new SQLExpression(
                        node.Method.ReturnType,
                        visitor.IdentifierIndex.Index++,
                        $"({arguments[0].Sql} >= 'A' AND {arguments[0].Sql} <= 'Z')",
                        arguments[0].Parameters
                    );
            }
        }

        if (CheckConstantMethod<char>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        return node;
    }

    public Expression HandleIntegerMethod(MethodCallExpression node)
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

            if (node.Method.Name == nameof(int.ToString))
            {
                return new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex.Index++,
                    $"CAST({obj.Sql} AS TEXT)",
                    obj.Parameters
                );
            }
        }

        if (CheckConstantMethod<long>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        if (node.Method.Name == "Parse")
        {
            if (arguments[0].SQLExpression == null)
            {
                return Expression.Call(node.Method, arguments.Select(f => f.Expression));
            }

            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"CAST({arguments[0].Sql} AS INTEGER)",
                arguments[0].Parameters
            );
        }

        return node;
    }

    public Expression HandleFloatingPointMethod(MethodCallExpression node)
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

            if (node.Method.Name == nameof(double.ToString))
            {
                return new SQLExpression(
                    node.Method.ReturnType,
                    visitor.IdentifierIndex.Index++,
                    $"CAST({obj.Sql} AS TEXT)",
                    obj.Parameters
                );
            }
        }

        if (CheckConstantMethod<double>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        if (node.Method.Name == "Parse")
        {
            if (arguments[0].SQLExpression == null)
            {
                return Expression.Call(node.Method, arguments.Select(f => f.Expression));
            }

            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"CAST({arguments[0].Sql} AS REAL)",
                arguments[0].Parameters
            );
        }

        return node;
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

            return new SQLExpression(method.ReturnType, visitor.IdentifierIndex.Index++, $"{obj.Sql} LIKE {pName} {rest}", parameters);
        }
        else
        {
            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj, arguments[0].SQLExpression!);

            return new SQLExpression(
                method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"{obj.Sql} LIKE {selectValue(arguments[0].SQLExpression!)} {rest}",
                parameters
            );
        }
    }

    private Expression ResolveTrim(MethodCallExpression node, SQLExpression obj, List<ResolvedModel> arguments, string trimType)
    {
        if (arguments.Count == 0)
        {
            return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex.Index++, $"{trimType}({obj.Sql})", obj.Parameters);
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

            string concatenatedChars = string.Join(" || ", args.Select(f => f.Sql));

            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters([obj, .. args.Select(f => f.SQLExpression!)]);
            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"{trimType}({obj.Sql}, {concatenatedChars})",
                parameters
            );
        }
        else
        {
            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj, arguments[0].SQLExpression!);

            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
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
            visitor.IdentifierIndex.Index++,
            $"CAST({obj.Sql} + ({arguments[0].Sql} * {parameter.Name}) AS 'INTEGER')",
            [.. obj.Parameters ?? [], .. arguments[0].Parameters ?? [], parameter]
        );
    }

    private SQLExpression ResolveParse(MethodInfo method, List<ResolvedModel> arguments, long multiplyBy)
    {
        return new SQLExpression(
            method.ReturnType,
            visitor.IdentifierIndex.Index++,
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

            return new SQLExpression(method.ReturnType, visitor.IdentifierIndex.Index++, sql, parameters);
        }
        else
        {
            SQLiteParameter[] parameters = [.. obj.Parameters ?? [], .. arguments[0].Parameters ?? [], tickParameter, tickToSecondParameter];

            string sql = $"CAST(STRFTIME('%s',DATETIME(({obj.Sql} - {tickParameter.Name}) / {tickToSecondParameter.Name}, 'unixepoch', '+'||{arguments[0].Sql}||' {addType}')) AS INTEGER) * {tickToSecondParameter.Name} + {tickParameter.Name}";

            return new SQLExpression(
                method.ReturnType,
                visitor.IdentifierIndex.Index++,
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
                visitor.IdentifierIndex.Index++,
                $"{aggregateFunction}({sqlExpression!.Sql})",
                sqlExpression.Parameters
            );
        }

        LambdaExpression lambda = (LambdaExpression)CommonHelpers.StripQuotes(node.Arguments[1]);
        Expression resolvedExpression = visitor.Visit(lambda.Body);
        if (resolvedExpression is not SQLExpression sql)
        {
            throw new NotSupportedException("Sum could not resolve the expression.");
        }

        return new SQLExpression(
            node.Method.ReturnType,
            visitor.IdentifierIndex.Index++,
            $"{aggregateFunction}({sql.Sql})",
            sql.Parameters
        );
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
            expression = new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex.Index++, pName, result);
            return true;
        }

        expression = null;
        return false;
    }
}