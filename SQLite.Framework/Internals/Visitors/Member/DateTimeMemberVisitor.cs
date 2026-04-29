namespace SQLite.Framework.Internals.Visitors;

internal static class DateTimeMemberVisitor
{
    public static Expression HandleDateTimeMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLiteExpression == null || arguments.Any(f => f.SQLiteExpression == null) || node.Method.Name == nameof(DateTime.ToString))
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
                nameof(DateTime.Add) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, 1),
                nameof(DateTime.AddYears) => ResolveRelativeDate(visitor, node.Method, obj.SQLiteExpression, arguments, "years"),
                nameof(DateTime.AddMonths) => ResolveRelativeDate(visitor, node.Method, obj.SQLiteExpression, arguments, "months"),
                nameof(DateTime.AddDays) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerDay),
                nameof(DateTime.AddHours) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerHour),
                nameof(DateTime.AddMinutes) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerMinute),
                nameof(DateTime.AddSeconds) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerSecond),
                nameof(DateTime.AddMilliseconds) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerMillisecond),
                nameof(DateTime.AddMicroseconds) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerMicrosecond),
                nameof(DateTime.AddTicks) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, 1),
                _ => throw new NotSupportedException($"DateTime.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (QueryableMemberVisitor.CheckConstantMethod<DateTime>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        throw new NotSupportedException($"DateTime.{node.Method.Name} is not translatable to SQL.");
    }

    public static Expression HandleDateTimeOffsetMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLiteExpression == null || arguments.Any(f => f.SQLiteExpression == null) || node.Method.Name == nameof(DateTimeOffset.ToString))
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
                nameof(DateTimeOffset.Add) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, 1),
                nameof(DateTimeOffset.AddYears) => ResolveRelativeDate(visitor, node.Method, obj.SQLiteExpression, arguments, "years"),
                nameof(DateTimeOffset.AddMonths) => ResolveRelativeDate(visitor, node.Method, obj.SQLiteExpression, arguments, "months"),
                nameof(DateTimeOffset.AddDays) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerDay),
                nameof(DateTimeOffset.AddHours) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerHour),
                nameof(DateTimeOffset.AddMinutes) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerMinute),
                nameof(DateTimeOffset.AddSeconds) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerSecond),
                nameof(DateTimeOffset.AddMilliseconds) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerMillisecond),
                nameof(DateTimeOffset.AddMicroseconds) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerMicrosecond),
                nameof(DateTimeOffset.AddTicks) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, 1),
                _ => throw new NotSupportedException($"DateTimeOffset.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (QueryableMemberVisitor.CheckConstantMethod<DateTimeOffset>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        throw new NotSupportedException($"DateTimeOffset.{node.Method.Name} is not translatable to SQL.");
    }

    public static Expression HandleTimeSpanMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLiteExpression == null || arguments.Any(f => f.SQLiteExpression == null) || node.Method.Name == nameof(TimeSpan.ToString))
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
                nameof(TimeSpan.Add) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, 1),
                nameof(TimeSpan.Subtract) => new SQLiteExpression(
                    node.Method.ReturnType,
                    visitor.Counters.IdentifierIndex++,
                    $"{obj.Sql} - {arguments[0].Sql}",
                    ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!)
                ),
                nameof(TimeSpan.Negate) => new SQLiteExpression(
                    node.Method.ReturnType,
                    visitor.Counters.IdentifierIndex++,
                    $"(-{obj.Sql})",
                    obj.Parameters
                ),
                nameof(TimeSpan.Duration) => new SQLiteExpression(
                    node.Method.ReturnType,
                    visitor.Counters.IdentifierIndex++,
                    $"ABS({obj.Sql})",
                    obj.Parameters
                ),
                _ => throw new NotSupportedException($"TimeSpan.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (QueryableMemberVisitor.CheckConstantMethod<TimeSpan>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        return node.Method.Name switch
        {
            nameof(TimeSpan.FromDays) => ResolveParse(visitor, node.Method, arguments, TimeSpan.TicksPerDay),
            nameof(TimeSpan.FromHours) => ResolveParse(visitor, node.Method, arguments, TimeSpan.TicksPerHour),
            nameof(TimeSpan.FromMinutes) => ResolveParse(visitor, node.Method, arguments, TimeSpan.TicksPerMinute),
            nameof(TimeSpan.FromSeconds) => ResolveParse(visitor, node.Method, arguments, TimeSpan.TicksPerSecond),
            nameof(TimeSpan.FromMilliseconds) => ResolveParse(visitor, node.Method, arguments, TimeSpan.TicksPerMillisecond),
            nameof(TimeSpan.FromMicroseconds) => ResolveParse(visitor, node.Method, arguments, TimeSpan.TicksPerMicrosecond),
            nameof(TimeSpan.FromTicks) => ResolveParse(visitor, node.Method, arguments, 1),
            _ => throw new NotSupportedException($"TimeSpan.{node.Method.Name} is not translatable to SQL.")
        };
    }

    public static Expression HandleDateOnlyMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLiteExpression == null || arguments.Any(f => f.SQLiteExpression == null) || node.Method.Name == nameof(DateOnly.ToString)
                || visitor.Database.Options.DateOnlyStorage == DateOnlyStorageMode.Text)
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(DateOnly.AddYears) => ResolveRelativeDate(visitor, node.Method, obj.SQLiteExpression, arguments, "years"),
                nameof(DateOnly.AddMonths) => ResolveRelativeDate(visitor, node.Method, obj.SQLiteExpression, arguments, "months"),
                nameof(DateOnly.AddDays) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerDay),
                _ => throw new NotSupportedException($"DateOnly.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (QueryableMemberVisitor.CheckConstantMethod<DateOnly>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        throw new NotSupportedException($"DateOnly.{node.Method.Name} is not translatable to SQL.");
    }

    public static Expression HandleTimeOnlyMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLiteExpression == null || arguments.Any(f => f.SQLiteExpression == null) || node.Method.Name == nameof(TimeOnly.ToString)
                || visitor.Database.Options.TimeOnlyStorage == TimeOnlyStorageMode.Text)
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            return node.Method.Name switch
            {
                nameof(TimeOnly.Add) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, 1),
                nameof(TimeOnly.AddHours) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerHour),
                nameof(TimeOnly.AddMinutes) => ResolveDateAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerMinute),
                _ => throw new NotSupportedException($"TimeOnly.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (QueryableMemberVisitor.CheckConstantMethod<TimeOnly>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        throw new NotSupportedException($"TimeOnly.{node.Method.Name} is not translatable to SQL.");
    }

    private static SQLiteExpression ResolveDateAdd(SQLVisitor visitor, MethodInfo method, SQLiteExpression obj, List<ResolvedModel> arguments, long multiplyBy)
    {
        SQLiteParameter parameter = new()
        {
            Name = $"@p{visitor.Counters.ParamIndex++}",
            Value = multiplyBy
        };

        return new SQLiteExpression(
            method.ReturnType,
            visitor.Counters.IdentifierIndex++,
            $"CAST({obj.Sql} + ({arguments[0].Sql} * {parameter.Name}) AS 'INTEGER')",
            [.. obj.Parameters ?? [], .. arguments[0].Parameters ?? [], parameter]
        );
    }

    private static SQLiteExpression ResolveParse(SQLVisitor visitor, MethodInfo method, List<ResolvedModel> arguments, long multiplyBy)
    {
        return new SQLiteExpression(
            method.ReturnType,
            visitor.Counters.IdentifierIndex++,
            $"CAST({multiplyBy} * {arguments[0].Sql} AS INTEGER)",
            arguments[0].Parameters
        );
    }

    private static SQLiteExpression ResolveRelativeDate(SQLVisitor visitor, MethodInfo method, SQLiteExpression obj, List<ResolvedModel> arguments, string addType)
    {
        (SQLiteParameter tickParameter, SQLiteParameter tickToSecondParameter) = CreateHelperDateParameters(visitor);

        if (arguments[0].IsConstant)
        {
            SQLiteParameter parameter = new()
            {
                Name = $"@p{visitor.Counters.ParamIndex++}",
                Value = $"+{arguments[0].Constant} {addType}"
            };

            SQLiteParameter[] parameters = [.. obj.Parameters ?? [], tickParameter, tickToSecondParameter, parameter];

            string sql = $"CAST(STRFTIME('%s',DATETIME(({obj.Sql} - {tickParameter.Name}) / {tickToSecondParameter.Name}, 'unixepoch', {parameter.Name})) AS INTEGER) * {tickToSecondParameter.Name} + {tickParameter.Name}";

            return new SQLiteExpression(method.ReturnType, visitor.Counters.IdentifierIndex++, sql, parameters);
        }
        else
        {
            SQLiteParameter[] parameters = [.. obj.Parameters ?? [], .. arguments[0].Parameters ?? [], tickParameter, tickToSecondParameter];

            string sql = $"CAST(STRFTIME('%s',DATETIME(({obj.Sql} - {tickParameter.Name}) / {tickToSecondParameter.Name}, 'unixepoch', '+'||{arguments[0].Sql}||' {addType}')) AS INTEGER) * {tickToSecondParameter.Name} + {tickParameter.Name}";

            return new SQLiteExpression(
                method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                sql,
                parameters
            );
        }
    }

    private static (SQLiteParameter TickParameter, SQLiteParameter TickToSecondParameter) CreateHelperDateParameters(SQLVisitor visitor)
    {
        SQLiteParameter tickParameter = new()
        {
            Name = $"@p{visitor.Counters.ParamIndex++}",
            Value = 621355968000000000 // new DateTime(1970, 1, 1).Ticks
        };
        SQLiteParameter tickToSecondParameter = new()
        {
            Name = $"@p{visitor.Counters.ParamIndex++}",
            Value = TimeSpan.TicksPerSecond
        };

        return (tickParameter, tickToSecondParameter);
    }

    public static Expression HandleDateTimeProperty(SQLVisitor visitor, string propertyName, Type type, SQLiteExpression node)
    {
        if (visitor.Database.Options.DateTimeStorage == DateTimeStorageMode.TextFormatted)
        {
            throw new NotSupportedException(
                $"DateTime.{propertyName} cannot be used in a LINQ query when DateTimeStorage is set to TextFormatted." +
                $" Use direct SQL queries instead, or switch to Integer storage.");
        }

        return propertyName switch
        {
            nameof(DateTime.Year) => ResolveDateFormat(visitor, type, node, "Y", "DATETIME"),
            nameof(DateTime.Month) => ResolveDateFormat(visitor, type, node, "m", "DATETIME"),
            nameof(DateTime.Day) => ResolveDateFormat(visitor, type, node, "d", "DATETIME"),
            nameof(DateTime.Hour) => ResolveDateFormat(visitor, type, node, "H", "DATETIME"),
            nameof(DateTime.Minute) => ResolveDateFormat(visitor, type, node, "M", "DATETIME"),
            nameof(DateTime.Second) => ResolveDateFormat(visitor, type, node, "S", "DATETIME"),
            nameof(DateTime.Millisecond) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"({node.Sql} / {TimeSpan.TicksPerMillisecond}) % 1000",
                node.Parameters
            ),
            nameof(DateTime.Ticks) => node,
            nameof(DateTime.DayOfWeek) => ResolveDateFormat(visitor, type, node, "w", "DATETIME"),
            nameof(DateTime.DayOfYear) => ResolveDateFormat(visitor, type, node, "j", "DATETIME"),
            _ => node
        };
    }

    public static Expression HandleDateTimeOffsetProperty(SQLVisitor visitor, string propertyName, Type type, SQLiteExpression node)
    {
        if (visitor.Database.Options.DateTimeOffsetStorage == DateTimeOffsetStorageMode.TextFormatted)
        {
            throw new NotSupportedException(
                $"DateTimeOffset.{propertyName} cannot be used in a LINQ query when DateTimeOffsetStorage is set to TextFormatted." +
                $" Use direct SQL queries instead, or switch to Ticks storage.");
        }

        return propertyName switch
        {
            nameof(DateTimeOffset.Year) => ResolveDateFormat(visitor, type, node, "Y", "DATETIME"),
            nameof(DateTimeOffset.Month) => ResolveDateFormat(visitor, type, node, "m", "DATETIME"),
            nameof(DateTimeOffset.Day) => ResolveDateFormat(visitor, type, node, "d", "DATETIME"),
            nameof(DateTimeOffset.Hour) => ResolveDateFormat(visitor, type, node, "H", "DATETIME"),
            nameof(DateTimeOffset.Minute) => ResolveDateFormat(visitor, type, node, "M", "DATETIME"),
            nameof(DateTimeOffset.Second) => ResolveDateFormat(visitor, type, node, "S", "DATETIME"),
            nameof(DateTimeOffset.Millisecond) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"({node.Sql} / {TimeSpan.TicksPerMillisecond}) % 1000",
                node.Parameters
            ),
            nameof(DateTimeOffset.Ticks) => node,
            nameof(DateTimeOffset.DayOfWeek) => ResolveDateFormat(visitor, type, node, "w", "DATETIME"),
            nameof(DateTimeOffset.DayOfYear) => ResolveDateFormat(visitor, type, node, "j", "DATETIME"),
            _ => node
        };
    }

    public static Expression HandleTimeSpanProperty(SQLVisitor visitor, string propertyName, Type type, SQLiteExpression node)
    {
        if (visitor.Database.Options.TimeSpanStorage == TimeSpanStorageMode.Text)
        {
            throw new NotSupportedException(
                $"TimeSpan.{propertyName} cannot be used in a LINQ query when TimeSpanStorage is set to Text." +
                $" Use direct SQL queries instead, or switch to Integer storage.");
        }

        return propertyName switch
        {
            nameof(TimeSpan.Days) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"CAST({node.Sql} / {TimeSpan.TicksPerDay} AS INTEGER)",
                node.Parameters
            ),
            nameof(TimeSpan.TotalDays) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"(CAST({node.Sql} AS REAL) / {TimeSpan.TicksPerDay})",
                node.Parameters
            ),
            nameof(TimeSpan.Hours) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"({node.Sql} / {TimeSpan.TicksPerHour}) % 24",
                node.Parameters
            ),
            nameof(TimeSpan.TotalHours) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"(CAST({node.Sql} AS REAL) / {TimeSpan.TicksPerHour})",
                node.Parameters
            ),
            nameof(TimeSpan.Minutes) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"({node.Sql} / {TimeSpan.TicksPerMinute}) % 60",
                node.Parameters
            ),
            nameof(TimeSpan.TotalMinutes) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"(CAST({node.Sql} AS REAL) / {TimeSpan.TicksPerMinute})",
                node.Parameters
            ),
            nameof(TimeSpan.Seconds) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"({node.Sql} / {TimeSpan.TicksPerSecond}) % 60",
                node.Parameters
            ),
            nameof(TimeSpan.TotalSeconds) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"(CAST({node.Sql} AS REAL) / {TimeSpan.TicksPerSecond})",
                node.Parameters
            ),
            nameof(TimeSpan.Milliseconds) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"({node.Sql} / {TimeSpan.TicksPerMillisecond}) % 1000",
                node.Parameters
            ),
            nameof(TimeSpan.TotalMilliseconds) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"(CAST({node.Sql} AS REAL) / {TimeSpan.TicksPerMillisecond})",
                node.Parameters
            ),
            _ => node
        };
    }

    public static Expression HandleDateOnlyProperty(SQLVisitor visitor, string propertyName, Type type, SQLiteExpression node)
    {
        if (visitor.Database.Options.DateOnlyStorage == DateOnlyStorageMode.Text)
        {
            throw new NotSupportedException(
                $"DateOnly.{propertyName} cannot be used in a LINQ query when DateOnlyStorage is set to Text." +
                $" Use direct SQL queries instead, or switch to Integer storage.");
        }

        return propertyName switch
        {
            nameof(DateOnly.Year) => ResolveDateFormat(visitor, type, node, "Y", "DATE"),
            nameof(DateOnly.Month) => ResolveDateFormat(visitor, type, node, "m", "DATE"),
            nameof(DateOnly.Day) => ResolveDateFormat(visitor, type, node, "d", "DATE"),
            nameof(DateTime.DayOfWeek) => ResolveDateFormat(visitor, type, node, "w", "DATE"),
            nameof(DateTime.DayOfYear) => ResolveDateFormat(visitor, type, node, "j", "DATE"),
            _ => node
        };
    }

    public static Expression HandleTimeOnlyProperty(SQLVisitor visitor, string propertyName, Type type, SQLiteExpression node)
    {
        if (visitor.Database.Options.TimeOnlyStorage == TimeOnlyStorageMode.Text)
        {
            throw new NotSupportedException(
                $"TimeOnly.{propertyName} cannot be used in a LINQ query when TimeOnlyStorage is set to Text." +
                $" Use direct SQL queries instead, or switch to Integer storage.");
        }

        return propertyName switch
        {
            nameof(TimeOnly.Hour) => ResolveTimeFormat(visitor, type, node, "H"),
            nameof(TimeOnly.Minute) => ResolveTimeFormat(visitor, type, node, "M"),
            nameof(TimeOnly.Second) => ResolveTimeFormat(visitor, type, node, "S"),
            _ => node
        };
    }

    private static SQLiteExpression ResolveDateFormat(SQLVisitor visitor, Type type, SQLiteExpression obj, string format, string function)
    {
        (SQLiteParameter tickParameter, SQLiteParameter tickToSecondParameter) = CreateHelperDateParameters(visitor);

        return new SQLiteExpression(
            type,
            visitor.Counters.IdentifierIndex++,
            $"CAST(STRFTIME('%{format}',{function}(({obj.Sql} - {tickParameter.Name}) / {tickToSecondParameter.Name}, 'unixepoch')) AS INTEGER)",
            [.. obj.Parameters ?? [], tickParameter, tickToSecondParameter]
        );
    }

    private static SQLiteExpression ResolveTimeFormat(SQLVisitor visitor, Type type, SQLiteExpression obj, string format)
    {
        SQLiteParameter tickToSecondParameter = new()
        {
            Name = $"@p{visitor.Counters.ParamIndex++}",
            Value = TimeSpan.TicksPerSecond
        };
        return new SQLiteExpression(
            type,
            visitor.Counters.IdentifierIndex++,
            $"CAST(STRFTIME('%{format}',TIME({obj.Sql} / {tickToSecondParameter.Name}, 'unixepoch')) AS INTEGER)",
            [.. obj.Parameters ?? [], tickToSecondParameter]
        );
    }

}
