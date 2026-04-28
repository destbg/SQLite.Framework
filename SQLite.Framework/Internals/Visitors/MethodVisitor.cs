using System.Globalization;
using System.Text;

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
                        $"CASE WHEN LENGTH({arguments[0].Sql}) = 0 THEN LENGTH({obj.Sql}) ELSE COALESCE((WITH RECURSIVE find_pos(pos, rem) AS (SELECT 0, {obj.Sql} UNION ALL SELECT pos + INSTR(rem, {arguments[0].Sql}), SUBSTR(rem, INSTR(rem, {arguments[0].Sql}) + 1) FROM find_pos WHERE INSTR(rem, {arguments[0].Sql}) > 0) SELECT MAX(pos) - 1 FROM find_pos WHERE pos > 0), -1) END",
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
                            $"(CASE WHEN LENGTH({obj.Sql}) >= {arguments[0].Sql} THEN {obj.Sql} ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB({arguments[0].Sql} - LENGTH({obj.Sql}))), '00', {spaceParam.Name}), 1, {arguments[0].Sql} - LENGTH({obj.Sql})) || {obj.Sql}) END)",
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
                            $"(CASE WHEN LENGTH({obj.Sql}) >= {arguments[0].Sql} THEN {obj.Sql} ELSE ({obj.Sql} || (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB({arguments[0].Sql} - LENGTH({obj.Sql}))), '00', {spaceParam.Name}), 1, {arguments[0].Sql} - LENGTH({obj.Sql})))) END)",
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

                throw new NotSupportedException("string.Join with a non-array source is not translatable to SQL.");
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
                throw new NotSupportedException($"string.{node.Method.Name} is not translatable to SQL.");
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
            nameof(Math.Truncate) => new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"TRUNC({arguments[0].Sql})",
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
            _ => throw new NotSupportedException($"Math.{node.Method.Name} is not translatable to SQL.")
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

            if (visitor.Database.Options.DateTimeStorage == DateTimeStorageMode.TextFormatted)
            {
                if (visitor.IsInSelectProjection && visitor.Level == 0)
                {
                    return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
                }

                throw new NotSupportedException(
                    $"DateTime.{node.Method.Name} cannot be used in a LINQ query when DateTimeStorage is set to TextFormatted." +
                    $" Use direct SQL queries instead, or switch to Integer storage.");
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
                _ => throw new NotSupportedException($"DateTime.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (CheckConstantMethod<DateTime>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        throw new NotSupportedException($"DateTime.{node.Method.Name} is not translatable to SQL.");
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

            if (visitor.Database.Options.DateTimeOffsetStorage == DateTimeOffsetStorageMode.TextFormatted)
            {
                if (visitor.IsInSelectProjection && visitor.Level == 0)
                    return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
                throw new NotSupportedException(
                    $"DateTimeOffset.{node.Method.Name} cannot be used in a LINQ query when DateTimeOffsetStorage is set to TextFormatted." +
                    $" Use direct SQL queries instead, or switch to Ticks storage.");
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
                _ => throw new NotSupportedException($"DateTimeOffset.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (CheckConstantMethod<DateTimeOffset>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        throw new NotSupportedException($"DateTimeOffset.{node.Method.Name} is not translatable to SQL.");
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

            if (visitor.Database.Options.TimeSpanStorage == TimeSpanStorageMode.Text)
            {
                if (visitor.IsInSelectProjection && visitor.Level == 0)
                {
                    return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
                }

                throw new NotSupportedException(
                    $"TimeSpan.{node.Method.Name} cannot be used in a LINQ query when TimeSpanStorage is set to Text." +
                    $" Use direct SQL queries instead, or switch to Integer storage.");
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
                _ => throw new NotSupportedException($"TimeSpan.{node.Method.Name} is not translatable to SQL.")
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
            _ => throw new NotSupportedException($"TimeSpan.{node.Method.Name} is not translatable to SQL.")
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

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null) || node.Method.Name == nameof(DateOnly.ToString)
                || visitor.Database.Options.DateOnlyStorage == DateOnlyStorageMode.Text)
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(DateOnly.AddYears) => ResolveRelativeDate(node.Method, obj.SQLExpression, arguments, "years"),
                nameof(DateOnly.AddMonths) => ResolveRelativeDate(node.Method, obj.SQLExpression, arguments, "months"),
                nameof(DateOnly.AddDays) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerDay),
                _ => throw new NotSupportedException($"DateOnly.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (CheckConstantMethod<DateOnly>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        throw new NotSupportedException($"DateOnly.{node.Method.Name} is not translatable to SQL.");
    }

    public Expression HandleTimeOnlyMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLExpression == null || arguments.Any(f => f.SQLExpression == null) || node.Method.Name == nameof(TimeOnly.ToString)
                || visitor.Database.Options.TimeOnlyStorage == TimeOnlyStorageMode.Text)
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(TimeOnly.Add) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, 1),
                nameof(TimeOnly.AddHours) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerHour),
                nameof(TimeOnly.AddMinutes) => ResolveDateAdd(node.Method, obj.SQLExpression, arguments, TimeSpan.TicksPerMinute),
                _ => throw new NotSupportedException($"TimeOnly.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (CheckConstantMethod<TimeOnly>(node, arguments, out Expression? expression))
        {
            return expression;
        }

        throw new NotSupportedException($"TimeOnly.{node.Method.Name} is not translatable to SQL.");
    }

    public Expression HandleSQLiteFunctionsMethod(MethodCallExpression node)
    {
        return node.Method.Name switch
        {
            nameof(SQLiteFunctions.Random) => new SQLExpression(typeof(double), visitor.IdentifierIndex.Index++, "RANDOM()", null),
            nameof(SQLiteFunctions.RandomBlob) => HandleFunctionsRandomBlob(node),
            nameof(SQLiteFunctions.Glob) => HandleFunctionsGlob(node),
            nameof(SQLiteFunctions.UnixEpoch) => HandleFunctionsUnixEpoch(node),
            nameof(SQLiteFunctions.Printf) => HandleFunctionsPrintf(node),
            nameof(SQLiteFunctions.Regexp) => HandleFunctionsRegexp(node),
            nameof(SQLiteFunctions.Between) => HandleFunctionsBetween(node),
            nameof(SQLiteFunctions.In) => HandleFunctionsIn(node),
            nameof(SQLiteFunctions.Coalesce) => HandleFunctionsVariadic(node, "coalesce", node.Method.ReturnType),
            nameof(SQLiteFunctions.Nullif) => HandleFunctionsNullif(node),
            nameof(SQLiteFunctions.Typeof) => HandleFunctionsUnaryFn(node, "typeof", typeof(string)),
            nameof(SQLiteFunctions.Hex) => HandleFunctionsUnaryFn(node, "hex", typeof(string)),
            nameof(SQLiteFunctions.Quote) => HandleFunctionsUnaryFn(node, "quote", typeof(string)),
            nameof(SQLiteFunctions.Zeroblob) => HandleFunctionsUnaryFn(node, "zeroblob", typeof(byte[])),
            nameof(SQLiteFunctions.Instr) => HandleFunctionsInstr(node),
            nameof(SQLiteFunctions.LastInsertRowId) => new SQLExpression(typeof(long), visitor.IdentifierIndex.Index++, "last_insert_rowid()", null),
            nameof(SQLiteFunctions.SqliteVersion) => new SQLExpression(typeof(string), visitor.IdentifierIndex.Index++, "sqlite_version()", null),
            nameof(SQLiteFunctions.Min) => HandleFunctionsVariadic(node, "min", node.Method.ReturnType),
            nameof(SQLiteFunctions.Max) => HandleFunctionsVariadic(node, "max", node.Method.ReturnType),
            nameof(SQLiteFunctions.Changes) => new SQLExpression(typeof(long), visitor.IdentifierIndex.Index++, "changes()", null),
            nameof(SQLiteFunctions.TotalChanges) => new SQLExpression(typeof(long), visitor.IdentifierIndex.Index++, "total_changes()", null),
            _ => throw new NotSupportedException($"SQLiteFunctions.{node.Method.Name} is not translatable to SQL."),
        };
    }

    public Expression HandleSQLiteFTS5FunctionsMethod(MethodCallExpression node)
    {
        return node.Method.Name switch
        {
            nameof(SQLiteFTS5Functions.Match) => HandleFTS5Match(node),
            nameof(SQLiteFTS5Functions.Rank) => HandleFTS5Rank(node),
            nameof(SQLiteFTS5Functions.Snippet) => HandleFTS5Snippet(node),
            nameof(SQLiteFTS5Functions.Highlight) => HandleFTS5Highlight(node),
            _ => throw new NotSupportedException($"SQLiteFTS5Functions.{node.Method.Name} is not translatable to SQL."),
        };
    }

    public Expression HandleSQLiteJsonFunctionsMethod(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments.Select(visitor.ResolveExpression).ToList();
        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(arguments
            .Select(a => a.SQLExpression)
            .Where(s => s != null)
            .Cast<SQLExpression>()
            .ToArray());

        string sql = node.Method.Name switch
        {
            nameof(SQLiteJsonFunctions.Extract) => $"json_extract({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteJsonFunctions.Set) => $"json_set({arguments[0].Sql}, {arguments[1].Sql}, {arguments[2].Sql})",
            nameof(SQLiteJsonFunctions.Insert) => $"json_insert({arguments[0].Sql}, {arguments[1].Sql}, {arguments[2].Sql})",
            nameof(SQLiteJsonFunctions.Replace) => $"json_replace({arguments[0].Sql}, {arguments[1].Sql}, {arguments[2].Sql})",
            nameof(SQLiteJsonFunctions.Remove) => $"json_remove({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteJsonFunctions.Type) => $"json_type({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteJsonFunctions.Valid) => $"json_valid({arguments[0].Sql})",
            nameof(SQLiteJsonFunctions.Patch) => $"json_patch({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteJsonFunctions.ArrayLength) when arguments.Count == 1 => $"json_array_length({arguments[0].Sql})",
            nameof(SQLiteJsonFunctions.ArrayLength) => $"json_array_length({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteJsonFunctions.Minify) => $"json({arguments[0].Sql})",
            nameof(SQLiteJsonFunctions.ToJsonb) => $"jsonb({arguments[0].Sql})",
            nameof(SQLiteJsonFunctions.ExtractJsonb) => $"jsonb_extract({arguments[0].Sql}, {arguments[1].Sql})",
            _ => throw new NotSupportedException($"SQLiteJsonFunctions.{node.Method.Name} is not translatable to SQL."),
        };

        return new SQLExpression(node.Type, visitor.IdentifierIndex.Index++, sql, parameters);
    }

    public Expression HandleWindowFunctionMethod(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(SQLiteFrameBoundary))
        {
            return HandleFrameBoundary(node);
        }

        return HandleWindowFunction(node);
    }

    private SQLExpression HandleFrameBoundary(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case nameof(SQLiteFrameBoundary.UnboundedPreceding):
                return new SQLExpression(node.Type, visitor.IdentifierIndex.Index++, "UNBOUNDED PRECEDING", null);
            case nameof(SQLiteFrameBoundary.CurrentRow):
                return new SQLExpression(node.Type, visitor.IdentifierIndex.Index++, "CURRENT ROW", null);
            case nameof(SQLiteFrameBoundary.UnboundedFollowing):
                return new SQLExpression(node.Type, visitor.IdentifierIndex.Index++, "UNBOUNDED FOLLOWING", null);
            case nameof(SQLiteFrameBoundary.Preceding):
            {
                ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
                return new SQLExpression(node.Type, visitor.IdentifierIndex.Index++, $"{arg.Sql} PRECEDING", arg.SQLExpression?.Parameters);
            }
            case nameof(SQLiteFrameBoundary.Following):
            {
                ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
                return new SQLExpression(node.Type, visitor.IdentifierIndex.Index++, $"{arg.Sql} FOLLOWING", arg.SQLExpression?.Parameters);
            }
            default:
                throw new NotSupportedException($"SQLiteFrameBoundary.{node.Method.Name} is not translatable to SQL.");
        }
    }

    private SQLExpression HandleWindowFunction(MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Arguments.Select(visitor.ResolveExpression).ToList();
        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(arguments
            .Select(a => a.SQLExpression)
            .Where(s => s != null)
            .Cast<SQLExpression>()
            .ToArray());

        string sql = node.Method.Name switch
        {
            nameof(SQLiteWindowFunctions.Sum) => $"SUM({arguments[0].Sql})",
            nameof(SQLiteWindowFunctions.Avg) => $"AVG({arguments[0].Sql})",
            nameof(SQLiteWindowFunctions.Min) => $"MIN({arguments[0].Sql})",
            nameof(SQLiteWindowFunctions.Max) => $"MAX({arguments[0].Sql})",
            nameof(SQLiteWindowFunctions.Count) when arguments.Count == 0 => "COUNT(*)",
            nameof(SQLiteWindowFunctions.Count) => $"COUNT({arguments[0].Sql})",
            nameof(SQLiteWindowFunctions.RowNumber) => "ROW_NUMBER()",
            nameof(SQLiteWindowFunctions.Rank) => "RANK()",
            nameof(SQLiteWindowFunctions.DenseRank) => "DENSE_RANK()",
            nameof(SQLiteWindowFunctions.PercentRank) => "PERCENT_RANK()",
            nameof(SQLiteWindowFunctions.CumeDist) => "CUME_DIST()",
            nameof(SQLiteWindowFunctions.NTile) => $"NTILE({arguments[0].Sql})",
            nameof(SQLiteWindowFunctions.Lag) when arguments.Count == 1 => $"LAG({arguments[0].Sql})",
            nameof(SQLiteWindowFunctions.Lag) when arguments.Count == 2 => $"LAG({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteWindowFunctions.Lag) => $"LAG({arguments[0].Sql}, {arguments[1].Sql}, {arguments[2].Sql})",
            nameof(SQLiteWindowFunctions.Lead) when arguments.Count == 1 => $"LEAD({arguments[0].Sql})",
            nameof(SQLiteWindowFunctions.Lead) when arguments.Count == 2 => $"LEAD({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteWindowFunctions.Lead) => $"LEAD({arguments[0].Sql}, {arguments[1].Sql}, {arguments[2].Sql})",
            nameof(SQLiteWindowFunctions.FirstValue) => $"FIRST_VALUE({arguments[0].Sql})",
            nameof(SQLiteWindowFunctions.LastValue) => $"LAST_VALUE({arguments[0].Sql})",
            nameof(SQLiteWindowFunctions.NthValue) => $"NTH_VALUE({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteWindowFunctions.Over) => $"{arguments[0].Sql} OVER ()",
            nameof(SQLiteWindowFunctions.PartitionBy) => $"{TrimClose(arguments[0].Sql!)} PARTITION BY {arguments[1].Sql})",
            nameof(SQLiteWindowFunctions.ThenPartitionBy) => $"{TrimClose(arguments[0].Sql!)}, {arguments[1].Sql})",
            nameof(SQLiteWindowFunctions.OrderBy) => $"{TrimClose(arguments[0].Sql!)} ORDER BY {arguments[1].Sql} ASC)",
            nameof(SQLiteWindowFunctions.OrderByDescending) => $"{TrimClose(arguments[0].Sql!)} ORDER BY {arguments[1].Sql} DESC)",
            nameof(SQLiteWindowFunctions.ThenOrderBy) => $"{TrimClose(arguments[0].Sql!)}, {arguments[1].Sql} ASC)",
            nameof(SQLiteWindowFunctions.ThenOrderByDescending) => $"{TrimClose(arguments[0].Sql!)}, {arguments[1].Sql} DESC)",
            nameof(SQLiteWindowFunctions.Rows) => $"{TrimClose(arguments[0].Sql!)} ROWS BETWEEN {arguments[1].Sql} AND {arguments[2].Sql})",
            nameof(SQLiteWindowFunctions.Range) => $"{TrimClose(arguments[0].Sql!)} RANGE BETWEEN {arguments[1].Sql} AND {arguments[2].Sql})",
            nameof(SQLiteWindowFunctions.Groups) => $"{TrimClose(arguments[0].Sql!)} GROUPS BETWEEN {arguments[1].Sql} AND {arguments[2].Sql})",
            _ => throw new NotSupportedException($"SQLiteWindowFunctions.{node.Method.Name} is not translatable to SQL."),
        };

        return new SQLExpression(node.Type, visitor.IdentifierIndex.Index++, sql, parameters);
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

        throw new NotSupportedException($"Guid.{node.Method.Name} is not translatable to SQL.");
    }

    public Expression HandleQueryableMethod(MethodCallExpression node)
    {
        SQLTranslator translator = visitor.CloneDeeper(visitor.Level + 1);
        SQLQuery query = translator.Translate(node);

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

        if (node.Arguments.Count == 1 || node.Method.Name != nameof(Queryable.Contains))
        {
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

        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters([innerSql, .. arguments.Select(f => f.SQLExpression!)]);

        return new SQLExpression(
            node.Method.ReturnType,
            visitor.IdentifierIndex.Index++,
            $"{arguments[0].Sql} IN ({Environment.NewLine}{query.Sql}{Environment.NewLine})",
            parameters
        );
    }

    public Expression HandleEnumerableMethod(MethodCallExpression node, IEnumerable enumerable, List<ResolvedModel> arguments)
    {
        if (arguments.Any(f => f.Sql == null))
        {
            return Expression.Call(node.Object, node.Method, arguments.Select(f => f.Expression));
        }

        if (node.Object == null
            && CommonHelpers.IsSimple(node.Method.ReturnType, visitor.Database.Options)
            && arguments.All(f => f.IsConstant))
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

        return Expression.Call(node.Object, node.Method, arguments.Select(f => f.Expression));
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
            _ => throw new NotSupportedException($"Grouping aggregate {node.Method.Name} is not translatable to SQL.")
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

                    SQLiteParameter[] parameters = obj.Parameters == null
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

        if (node.Method.Name == nameof(Enum.Parse))
        {
            Type enumType;
            ResolvedModel stringArg;

            if (node.Method.IsGenericMethod)
            {
                enumType = node.Method.GetGenericArguments()[0];
                stringArg = arguments[0];
            }
            else
            {
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

            SQLiteParameter[] allParams = stringArg.Parameters == null
                ? [.. parameters]
                : [.. stringArg.Parameters, .. parameters];

            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                caseExpression,
                allParams
            );
        }

        throw new NotSupportedException($"Enum.{node.Method.Name} is not translatable to SQL.");
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

        throw new NotSupportedException($"char.{node.Method.Name} is not translatable to SQL.");
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
            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"CAST({arguments[0].Sql} AS INTEGER)",
                arguments[0].Parameters
            );
        }

        throw new NotSupportedException($"{node.Method.DeclaringType?.Name}.{node.Method.Name} is not translatable to SQL.");
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
            return new SQLExpression(
                node.Method.ReturnType,
                visitor.IdentifierIndex.Index++,
                $"CAST({arguments[0].Sql} AS REAL)",
                arguments[0].Parameters
            );
        }

        throw new NotSupportedException($"{node.Method.DeclaringType?.Name}.{node.Method.Name} is not translatable to SQL.");
    }

    private SQLExpression HandleFunctionsRandomBlob(MethodCallExpression node)
    {
        ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
        return new SQLExpression(
            typeof(byte[]),
            visitor.IdentifierIndex.Index++,
            $"RANDOMBLOB({arg.Sql})",
            arg.Parameters);
    }

    private SQLExpression HandleFunctionsGlob(MethodCallExpression node)
    {
        ResolvedModel pattern = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[1]);
        return new SQLExpression(
            typeof(bool),
            visitor.IdentifierIndex.Index++,
            $"({value.Sql} GLOB {pattern.Sql})",
            CommonHelpers.CombineParameters(value.SQLExpression!, pattern.SQLExpression!));
    }

    private SQLExpression HandleFunctionsBetween(MethodCallExpression node)
    {
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel low = visitor.ResolveExpression(node.Arguments[1]);
        ResolvedModel high = visitor.ResolveExpression(node.Arguments[2]);
        return new SQLExpression(
            typeof(bool),
            visitor.IdentifierIndex.Index++,
            $"({value.Sql} BETWEEN {low.Sql} AND {high.Sql})",
            CommonHelpers.CombineParameters(value.SQLExpression!, low.SQLExpression!, high.SQLExpression!));
    }

    private SQLExpression HandleFunctionsIn(MethodCallExpression node)
    {
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[0]);
        List<ResolvedModel> items = ResolveVariadic(node.Arguments[1]);

        string itemsSql = string.Join(", ", items.Select(r => r.Sql));
        SQLExpression[] parts = [value.SQLExpression!, .. items.Select(r => r.SQLExpression!)];
        return new SQLExpression(
            typeof(bool),
            visitor.IdentifierIndex.Index++,
            $"({value.Sql} IN ({itemsSql}))",
            CommonHelpers.CombineParameters(parts));
    }

    private SQLExpression HandleFunctionsVariadic(MethodCallExpression node, string sqlFunction, Type returnType)
    {
        List<ResolvedModel> items = ResolveVariadic(node.Arguments[0]);
        string argsSql = string.Join(", ", items.Select(r => r.Sql));
        SQLExpression[] parts = items.Select(r => r.SQLExpression!).ToArray();
        return new SQLExpression(
            returnType,
            visitor.IdentifierIndex.Index++,
            $"{sqlFunction}({argsSql})",
            CommonHelpers.CombineParameters(parts));
    }

    private SQLExpression HandleFunctionsNullif(MethodCallExpression node)
    {
        ResolvedModel a = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel b = visitor.ResolveExpression(node.Arguments[1]);
        return new SQLExpression(
            node.Method.ReturnType,
            visitor.IdentifierIndex.Index++,
            $"nullif({a.Sql}, {b.Sql})",
            CommonHelpers.CombineParameters(a.SQLExpression!, b.SQLExpression!));
    }

    private SQLExpression HandleFunctionsUnaryFn(MethodCallExpression node, string sqlFunction, Type returnType)
    {
        ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
        return new SQLExpression(
            returnType,
            visitor.IdentifierIndex.Index++,
            $"{sqlFunction}({arg.Sql})",
            arg.Parameters);
    }

    private SQLExpression HandleFunctionsInstr(MethodCallExpression node)
    {
        ResolvedModel haystack = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel needle = visitor.ResolveExpression(node.Arguments[1]);
        return new SQLExpression(
            typeof(int),
            visitor.IdentifierIndex.Index++,
            $"instr({haystack.Sql}, {needle.Sql})",
            CommonHelpers.CombineParameters(haystack.SQLExpression!, needle.SQLExpression!));
    }

    private List<ResolvedModel> ResolveVariadic(Expression argument)
    {
        List<ResolvedModel> resolved = [];
        if (argument is NewArrayExpression arrayExpr)
        {
            foreach (Expression e in arrayExpr.Expressions)
            {
                resolved.Add(visitor.ResolveExpression(e));
            }
        }
        else
        {
            Array array = (Array)CommonHelpers.GetConstantValue(argument)!;
            Type elementType = argument.Type.GetElementType()!;
            foreach (object? item in array)
            {
                resolved.Add(visitor.ResolveExpression(Expression.Constant(item, elementType)));
            }
        }
        return resolved;
    }

    private SQLExpression HandleFunctionsUnixEpoch(MethodCallExpression node)
    {
        if (node.Arguments.Count == 0)
        {
            return new SQLExpression(typeof(long), visitor.IdentifierIndex.Index++, "unixepoch()", null);
        }

        ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
        return new SQLExpression(
            typeof(long),
            visitor.IdentifierIndex.Index++,
            $"unixepoch({arg.Sql})",
            arg.Parameters);
    }

    private SQLExpression HandleFunctionsPrintf(MethodCallExpression node)
    {
        ResolvedModel format = visitor.ResolveExpression(node.Arguments[0]);

        List<ResolvedModel> rest = [];
        if (node.Arguments[1] is NewArrayExpression arrayExpr)
        {
            foreach (Expression e in arrayExpr.Expressions)
            {
                rest.Add(visitor.ResolveExpression(e));
            }
        }

        string argsSql = rest.Count == 0
            ? string.Empty
            : ", " + string.Join(", ", rest.Select(r => r.Sql));

        SQLExpression[] all = [format.SQLExpression!, .. rest.Select(r => r.SQLExpression!)];
        return new SQLExpression(
            typeof(string),
            visitor.IdentifierIndex.Index++,
            $"printf({format.Sql}{argsSql})",
            CommonHelpers.CombineParameters(all));
    }

    private SQLExpression HandleFunctionsRegexp(MethodCallExpression node)
    {
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel pattern = visitor.ResolveExpression(node.Arguments[1]);
        return new SQLExpression(
            typeof(bool),
            visitor.IdentifierIndex.Index++,
            $"({value.Sql} REGEXP {pattern.Sql})",
            CommonHelpers.CombineParameters(value.SQLExpression!, pattern.SQLExpression!));
    }

    private SQLExpression HandleFTS5Match(MethodCallExpression node)
    {
        Expression first = node.Arguments[0];
        Expression second = node.Arguments[1];

        bool firstIsColumn = node.Method.GetParameters()[0].ParameterType == typeof(string);

        Type entityType;
        string? columnName = null;

        if (firstIsColumn)
        {
            if (first is MemberExpression me && me.Expression != null)
            {
                columnName = me.Member.Name;
                entityType = me.Expression.Type;
            }
            else if (first is UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression me2 } && me2.Expression != null)
            {
                columnName = me2.Member.Name;
                entityType = me2.Expression.Type;
            }
            else
            {
                throw new NotSupportedException("SQLiteFTS5Functions.Match column reference must be a direct property access like a.Title.");
            }
        }
        else
        {
            entityType = first.Type;
        }

        string tableName = ResolveFTS5TableName(entityType);

        if (second.Type == typeof(string))
        {
            object? value = CommonHelpers.GetConstantValue(second);
            string queryString = (string)(value ?? string.Empty);
            if (columnName != null)
            {
                queryString = "{" + columnName + "} : " + queryString;
            }

            string pName = $"@p{visitor.ParamIndex.Index++}";
            SQLiteParameter parameter = new() { Name = pName, Value = queryString };
            return new SQLExpression(typeof(bool), visitor.IdentifierIndex.Index++, $"\"{tableName}\" MATCH {pName}", [parameter]);
        }

        Expression body = UnwrapPredicateBody(second);
        List<FtsQueryPart> parts = CommonHelpers.RenderFTSMatch(body, visitor);
        return BuildFTS5MatchSql(tableName, columnName, parts);
    }

    private SQLExpression BuildFTS5MatchSql(string tableName, string? columnName, List<FtsQueryPart> parts)
    {
        bool hasDynamic = parts.Any(p => p.DynamicSql != null);

        if (!hasDynamic)
        {
            string body = string.Concat(parts.Select(p => p.LiteralText));
            string queryString = columnName != null ? "{" + columnName + "} : (" + body + ")" : body;
            string pName = $"@p{visitor.ParamIndex.Index++}";
            SQLiteParameter parameter = new() { Name = pName, Value = queryString };
            return new SQLExpression(typeof(bool), visitor.IdentifierIndex.Index++, $"\"{tableName}\" MATCH {pName}", [parameter]);
        }

        StringBuilder operand = new();
        List<SQLiteParameter> parameters = [];

        if (columnName != null)
        {
            string prefixLiteral = "{" + columnName + "} : (";
            AppendLiteralPart(operand, parameters, prefixLiteral);
        }

        for (int i = 0; i < parts.Count; i++)
        {
            FtsQueryPart part = parts[i];
            if (part.LiteralText != null)
            {
                AppendLiteralPart(operand, parameters, part.LiteralText);
            }
            else
            {
                AppendDynamicPart(operand, parameters, part.DynamicSql!);
            }
        }

        if (columnName != null)
        {
            AppendLiteralPart(operand, parameters, ")");
        }

        return new SQLExpression(typeof(bool), visitor.IdentifierIndex.Index++, $"\"{tableName}\" MATCH ({operand})", parameters.ToArray());
    }

    private void AppendLiteralPart(StringBuilder operand, List<SQLiteParameter> parameters, string text)
    {
        if (operand.Length > 0)
        {
            operand.Append(" || ");
        }

        string pName = $"@p{visitor.ParamIndex.Index++}";
        parameters.Add(new SQLiteParameter { Name = pName, Value = text });
        operand.Append(pName);
    }

    private static Expression UnwrapPredicateBody(Expression expr)
    {
        Expression stripped = CommonHelpers.StripQuotes(expr);
        if (stripped is LambdaExpression lambda)
        {
            return lambda.Body;
        }

        return expr;
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "FTS5 entity type is referenced by user code.")]
    private SQLExpression HandleFTS5Rank(MethodCallExpression node)
    {
        string alias = ResolveEntityAlias(node.Arguments[0]);
        Type entityType = node.Arguments[0].Type;
        TableMapping mapping = visitor.Database.TableMapping(entityType);

        if (mapping.FullTextSearch != null && mapping.FullTextSearch.IndexedColumns.Any(c => c.Weight != 1.0))
        {
            string weights = string.Join(", ", mapping.FullTextSearch.IndexedColumns
                .Select(c => c.Weight.ToString(CultureInfo.InvariantCulture)));
            return new SQLExpression(typeof(double), visitor.IdentifierIndex.Index++, $"bm25(\"{mapping.TableName}\", {weights})");
        }

        return new SQLExpression(typeof(double), visitor.IdentifierIndex.Index++, $"{alias}.rank");
    }

    private SQLExpression HandleFTS5Snippet(MethodCallExpression node)
    {
        Type entityType = node.Arguments[0].Type;
        string tableName = ResolveFTS5TableName(entityType);
        int columnIndex = ResolveFTS5ColumnIndex(entityType, node.Arguments[1]);

        SQLExpression before = ResolveAuxArg(node.Arguments[2]);
        SQLExpression after = ResolveAuxArg(node.Arguments[3]);
        SQLExpression ellipsis = ResolveAuxArg(node.Arguments[4]);
        SQLExpression tokens = ResolveAuxArg(node.Arguments[5]);

        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(before, after, ellipsis, tokens);
        return new SQLExpression(typeof(string), visitor.IdentifierIndex.Index++, $"snippet(\"{tableName}\", {columnIndex}, {before.Sql}, {after.Sql}, {ellipsis.Sql}, {tokens.Sql})", parameters);
    }

    private SQLExpression HandleFTS5Highlight(MethodCallExpression node)
    {
        Type entityType = node.Arguments[0].Type;
        string tableName = ResolveFTS5TableName(entityType);
        int columnIndex = ResolveFTS5ColumnIndex(entityType, node.Arguments[1]);

        SQLExpression before = ResolveAuxArg(node.Arguments[2]);
        SQLExpression after = ResolveAuxArg(node.Arguments[3]);

        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(before, after);
        return new SQLExpression(typeof(string), visitor.IdentifierIndex.Index++, $"highlight(\"{tableName}\", {columnIndex}, {before.Sql}, {after.Sql})", parameters);
    }

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "Entity type is referenced by user code via the LINQ expression.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Entity type is referenced by user code via the LINQ expression.")]
    private string ResolveFTS5TableName(Type entityType)
    {
        TableMapping mapping = visitor.Database.TableMapping(entityType);
        if (mapping.FullTextSearch == null)
        {
            throw new NotSupportedException($"SQLiteFTS5 method requires an entity with [FullTextSearch]; '{entityType.Name}' does not.");
        }

        return mapping.TableName;
    }

    private SQLExpression ResolveAuxArg(Expression expr)
    {
        return visitor.ResolveExpression(expr).SQLExpression!;
    }

    private string ResolveEntityAlias(Expression entity)
    {
        if (entity is ParameterExpression pe && visitor.MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? dict))
        {
            foreach (KeyValuePair<string, Expression> kv in dict)
            {
                if (kv.Value is SQLExpression sql)
                {
                    int dot = sql.Sql.IndexOf('.');
                    if (dot > 0)
                    {
                        return sql.Sql[..dot];
                    }
                }
            }
        }

        if (entity is MemberExpression member)
        {
            ResolvedModel resolved = visitor.ResolveExpression(member);
            if (resolved.SQLExpression != null)
            {
                int dot = resolved.SQLExpression.Sql.IndexOf('.');
                if (dot > 0)
                {
                    return resolved.SQLExpression.Sql[..dot];
                }
            }
        }

        throw new NotSupportedException($"SQLiteFTS5 method requires a direct entity reference; got {entity}.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "Entity type is referenced by user code via the LINQ expression.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Entity type is referenced by user code via the LINQ expression.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Entity type is referenced by user code.")]
    private int ResolveFTS5ColumnIndex(Type entityType, Expression columnArg)
    {
        TableMapping mapping = visitor.Database.TableMapping(entityType);
        if (mapping.FullTextSearch == null)
        {
            throw new NotSupportedException($"SQLiteFTS5 method requires an entity with [FullTextSearch]; '{entityType.Name}' does not.");
        }

        string columnName = columnArg switch
        {
            MemberExpression me => me.Member.Name,
            UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression me2 } => me2.Member.Name,
            _ => throw new NotSupportedException("SQLiteFTS5 column argument must be a direct property reference like a.Title.")
        };

        for (int i = 0; i < mapping.FullTextSearch.IndexedColumns.Count; i++)
        {
            if (mapping.FullTextSearch.IndexedColumns[i].Name == columnName || mapping.FullTextSearch.IndexedColumns[i].Property.Name == columnName)
            {
                return i;
            }
        }

        throw new NotSupportedException($"SQLiteFTS5 column '{columnName}' is not declared on FTS entity '{entityType.Name}'.");
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

            string pName = $"@p{visitor.ParamIndex.Index++}";
            expression = new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex.Index++, pName, result);
            return true;
        }

        expression = null;
        return false;
    }

    public Expression HandleCustomMethod(MethodCallExpression node, ResolvedModel? obj, List<ResolvedModel> arguments, SQLiteMethodTranslator translator)
    {
        if (arguments.Any(f => f.SQLExpression == null) || (obj != null && obj.SQLExpression == null))
        {
            return obj != null
                ? Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression))
                : Expression.Call(node.Method, arguments.Select(f => f.Expression));
        }

        string? instanceSql = obj?.SQLExpression?.Sql;
        string[] argumentsSql = arguments.Select(f => f.Sql!).ToArray();
        string sql = translator(instanceSql, argumentsSql);

        List<SQLExpression> allExpressions = arguments.Select(f => f.SQLExpression!).ToList();
        if (obj?.SQLExpression != null)
        {
            allExpressions.Insert(0, obj.SQLExpression);
        }

        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(allExpressions.ToArray());

        return new SQLExpression(node.Method.ReturnType, visitor.IdentifierIndex.Index++, sql, parameters);
    }

    private static string TrimClose(string sql)
    {
        return sql[..^1];
    }

    private static void AppendDynamicPart(StringBuilder operand, List<SQLiteParameter> parameters, SQLExpression sql)
    {
        if (operand.Length > 0)
        {
            operand.Append(" || ");
        }

        if (sql.Parameters != null)
        {
            parameters.AddRange(sql.Parameters);
        }

        operand.Append("printf('\"%w\"', ");
        operand.Append(sql.Sql);
        operand.Append(')');
    }
}