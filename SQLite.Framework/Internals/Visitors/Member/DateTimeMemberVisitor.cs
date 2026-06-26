namespace SQLite.Framework.Internals.Visitors.Member;

internal static class DateTimeMemberVisitor
{
    private static readonly long[] DescendingUnitTicks =
    [
        TimeSpan.TicksPerDay,
        TimeSpan.TicksPerHour,
        TimeSpan.TicksPerMinute,
        TimeSpan.TicksPerSecond,
        TimeSpan.TicksPerMillisecond,
        TimeSpan.TicksPerMicrosecond,
    ];

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
                _ => visitor.NotTranslatable(node, $"DateTime.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (QueryableMemberVisitor.CheckConstantMethod<DateTime>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        return visitor.NotTranslatable(node, $"DateTime.{node.Method.Name} is not translatable to SQL.");
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
                _ => visitor.NotTranslatable(node, $"DateTimeOffset.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (QueryableMemberVisitor.CheckConstantMethod<DateTimeOffset>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        return visitor.NotTranslatable(node, $"DateTimeOffset.{node.Method.Name} is not translatable to SQL.");
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
                nameof(TimeSpan.Subtract) => SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "", obj.SQLiteExpression!, " - ", arguments[0].SQLiteExpression!, "", ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!)),
                nameof(TimeSpan.Negate) => SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(-", obj.SQLiteExpression!, ")", obj.Parameters),
                nameof(TimeSpan.Duration) => SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "ABS(", obj.SQLiteExpression!, ")", obj.Parameters),
                _ => visitor.NotTranslatable(node, $"TimeSpan.{node.Method.Name} is not translatable to SQL.")
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
            _ => visitor.NotTranslatable(node, $"TimeSpan.{node.Method.Name} is not translatable to SQL.")
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
                _ => visitor.NotTranslatable(node, $"DateOnly.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (QueryableMemberVisitor.CheckConstantMethod<DateOnly>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        return visitor.NotTranslatable(node, $"DateOnly.{node.Method.Name} is not translatable to SQL.");
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
                nameof(TimeOnly.Add) => ResolveTimeOnlyAdd(visitor, node.Method, obj.SQLiteExpression, arguments, 1),
                nameof(TimeOnly.AddHours) => ResolveTimeOnlyAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerHour),
                nameof(TimeOnly.AddMinutes) => ResolveTimeOnlyAdd(visitor, node.Method, obj.SQLiteExpression, arguments, TimeSpan.TicksPerMinute),
                _ => visitor.NotTranslatable(node, $"TimeOnly.{node.Method.Name} is not translatable to SQL.")
            };
        }

        if (QueryableMemberVisitor.CheckConstantMethod<TimeOnly>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        return visitor.NotTranslatable(node, $"TimeOnly.{node.Method.Name} is not translatable to SQL.");
    }

    public static Expression HandleDateTimeProperty(SQLVisitor visitor, MemberExpression member, SQLiteExpression node)
    {
        string propertyName = member.Member.Name;
        Type type = member.Type;

        if (visitor.Database.Options.DateTimeStorage == DateTimeStorageMode.TextFormatted)
        {
            return visitor.NotTranslatable(member,
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
            nameof(DateTime.Millisecond) => DivModExpression(visitor, type, node, TimeSpan.TicksPerMillisecond, 1000),
            nameof(DateTime.Microsecond) => DivModExpression(visitor, type, node, TimeSpan.TicksPerMicrosecond, 1000),
            nameof(DateTime.Nanosecond) => ModMulExpression(visitor, type, node, TimeSpan.TicksPerMicrosecond, TimeSpan.NanosecondsPerTick),
            nameof(DateTime.Ticks) => node,
            nameof(DateTime.DayOfWeek) => ResolveDateFormat(visitor, type, node, "w", "DATETIME"),
            nameof(DateTime.DayOfYear) => ResolveDateFormat(visitor, type, node, "j", "DATETIME"),
            nameof(DateTime.Date) => DateTruncExpression(visitor, type, node),
            nameof(DateTime.TimeOfDay) => ModExpression(visitor, type, node, TimeSpan.TicksPerDay),
            _ => visitor.NotTranslatable(member, $"DateTime.{propertyName} is not translatable to SQL.")
        };
    }

    public static Expression HandleDateTimeOffsetProperty(SQLVisitor visitor, MemberExpression member, SQLiteExpression node)
    {
        string propertyName = member.Member.Name;
        Type type = member.Type;

        if (visitor.Database.Options.DateTimeOffsetStorage == DateTimeOffsetStorageMode.TextFormatted)
        {
            return visitor.NotTranslatable(member,
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
            nameof(DateTimeOffset.Millisecond) => DivModExpression(visitor, type, node, TimeSpan.TicksPerMillisecond, 1000),
            nameof(DateTimeOffset.Microsecond) => DivModExpression(visitor, type, node, TimeSpan.TicksPerMicrosecond, 1000),
            nameof(DateTimeOffset.Nanosecond) => ModMulExpression(visitor, type, node, TimeSpan.TicksPerMicrosecond, TimeSpan.NanosecondsPerTick),
            nameof(DateTimeOffset.Ticks) => node,
            nameof(DateTimeOffset.DayOfWeek) => ResolveDateFormat(visitor, type, node, "w", "DATETIME"),
            nameof(DateTimeOffset.DayOfYear) => ResolveDateFormat(visitor, type, node, "j", "DATETIME"),
            nameof(DateTimeOffset.Date) => DateTruncExpression(visitor, type, node),
            nameof(DateTimeOffset.TimeOfDay) => ModExpression(visitor, type, node, TimeSpan.TicksPerDay),
            _ => visitor.NotTranslatable(member, $"DateTimeOffset.{propertyName} is not translatable to SQL.")
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
            nameof(TimeSpan.Days) => DivExpression(visitor, type, node, TimeSpan.TicksPerDay),
            nameof(TimeSpan.TotalDays) => DivAsRealExpression(visitor, type, node, TimeSpan.TicksPerDay),
            nameof(TimeSpan.Hours) => DivModExpression(visitor, type, node, TimeSpan.TicksPerHour, 24),
            nameof(TimeSpan.TotalHours) => DivAsRealExpression(visitor, type, node, TimeSpan.TicksPerHour),
            nameof(TimeSpan.Minutes) => DivModExpression(visitor, type, node, TimeSpan.TicksPerMinute, 60),
            nameof(TimeSpan.TotalMinutes) => DivAsRealExpression(visitor, type, node, TimeSpan.TicksPerMinute),
            nameof(TimeSpan.Seconds) => DivModExpression(visitor, type, node, TimeSpan.TicksPerSecond, 60),
            nameof(TimeSpan.TotalSeconds) => DivAsRealExpression(visitor, type, node, TimeSpan.TicksPerSecond),
            nameof(TimeSpan.Milliseconds) => DivModExpression(visitor, type, node, TimeSpan.TicksPerMillisecond, 1000),
            nameof(TimeSpan.TotalMilliseconds) => DivAsRealExpression(visitor, type, node, TimeSpan.TicksPerMillisecond),
            nameof(TimeSpan.Microseconds) => DivModExpression(visitor, type, node, TimeSpan.TicksPerMicrosecond, 1000),
            nameof(TimeSpan.TotalMicroseconds) => DivAsRealExpression(visitor, type, node, TimeSpan.TicksPerMicrosecond),
            nameof(TimeSpan.Nanoseconds) => ModMulExpression(visitor, type, node, TimeSpan.TicksPerMicrosecond, TimeSpan.NanosecondsPerTick),
            nameof(TimeSpan.TotalNanoseconds) => MulAsRealExpression(visitor, type, node, TimeSpan.NanosecondsPerTick),
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
            nameof(DateOnly.DayNumber) => DivExpression(visitor, type, node, TimeSpan.TicksPerDay),
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
            nameof(TimeOnly.Millisecond) => DivModExpression(visitor, type, node, TimeSpan.TicksPerMillisecond, 1000),
            nameof(TimeOnly.Microsecond) => DivModExpression(visitor, type, node, TimeSpan.TicksPerMicrosecond, 1000),
            nameof(TimeOnly.Nanosecond) => ModMulExpression(visitor, type, node, TimeSpan.TicksPerMicrosecond, TimeSpan.NanosecondsPerTick),
            _ => node
        };
    }

    private static SQLiteExpression ResolveDateAdd(SQLVisitor visitor, MethodInfo method, SQLiteExpression obj, List<ResolvedModel> arguments, long multiplyBy)
    {
        SQLiteParameter parameter = new()
        {
            Name = visitor.Counters.NextParamName(),
            Value = multiplyBy
        };

        SQLiteExpression argExpression = arguments[0].IsConstant && arguments[0].Constant is TimeSpan ts
            ? SQLiteExpression.Leaf(typeof(long), visitor.Counters.NextIdentifier(), visitor.Counters.NextParamName(), ts.Ticks)
            : arguments[0].SQLiteExpression!;

        SQLiteParameter[] combinedParameters = [.. obj.Parameters ?? [], .. argExpression.Parameters ?? [], parameter];

        if (multiplyBy == 1)
        {
            return SQLiteExpression.Binary(
                method.ReturnType,
                visitor.Counters.NextIdentifier(),
                "CAST(", obj, " + (", argExpression, $" * {parameter.Name}) AS 'INTEGER')",
                combinedParameters
            );
        }

        return SQLiteExpression.Binary(
            method.ReturnType,
            visitor.Counters.NextIdentifier(),
            "(", obj, " + CAST((", argExpression, $") * {parameter.Name} AS INTEGER))",
            combinedParameters
        );
    }

    private static SQLiteExpression ResolveTimeOnlyAdd(SQLVisitor visitor, MethodInfo method, SQLiteExpression obj, List<ResolvedModel> arguments, long multiplyBy)
    {
        SQLiteParameter parameter = new()
        {
            Name = visitor.Counters.NextParamName(),
            Value = multiplyBy
        };

        SQLiteExpression argExpression = arguments[0].IsConstant && arguments[0].Constant is TimeSpan ts
            ? SQLiteExpression.Leaf(typeof(long), visitor.Counters.NextIdentifier(), visitor.Counters.NextParamName(), ts.Ticks)
            : arguments[0].SQLiteExpression!;

        long day = TimeSpan.TicksPerDay;
        return SQLiteExpression.Binary(
            method.ReturnType,
            visitor.Counters.NextIdentifier(),
            "CAST(((", obj, " + CAST((", argExpression, $") * {parameter.Name} AS INTEGER)) % {day} + {day}) % {day} AS 'INTEGER')",
            [.. obj.Parameters ?? [], .. argExpression.Parameters ?? [], parameter]
        );
    }

    private static SQLiteExpression ResolveParse(SQLVisitor visitor, MethodInfo method, List<ResolvedModel> arguments, long multiplyBy)
    {
        if (arguments.Count == 1)
        {
            return SQLiteExpression.Wrap(
                method.ReturnType,
                visitor.Counters.NextIdentifier(),
                $"CAST({multiplyBy} * ", arguments[0].SQLiteExpression!, " AS INTEGER)",
                arguments[0].Parameters
            );
        }

        int startIndex = Array.IndexOf(DescendingUnitTicks, multiplyBy);
        List<(long Unit, SQLiteExpression Expr)> terms = [];
        for (int i = 0; i < arguments.Count; i++)
        {
            ResolvedModel arg = arguments[i];
            if (arg.IsConstant && Convert.ToInt64(arg.Constant) == 0)
            {
                continue;
            }

            terms.Add((DescendingUnitTicks[startIndex + i], arg.SQLiteExpression!));
        }

        string[] parts = new string[terms.Count + 1];
        SQLiteExpression[] exprs = new SQLiteExpression[terms.Count];
        for (int i = 0; i < terms.Count; i++)
        {
            parts[i] = i == 0 ? $"CAST({terms[i].Unit} * (" : $") + {terms[i].Unit} * (";
            exprs[i] = terms[i].Expr;
        }

        parts[terms.Count] = ") AS INTEGER)";

        return SQLiteExpression.Multi(
            method.ReturnType,
            visitor.Counters.NextIdentifier(),
            parts,
            exprs,
            ParameterHelpers.CombineParameters(exprs)
        );
    }

    private static SQLiteExpression ResolveRelativeDate(SQLVisitor visitor, MethodInfo method, SQLiteExpression obj, List<ResolvedModel> arguments, string addType)
    {
        (SQLiteParameter tickParameter, SQLiteParameter tickToSecondParameter) = CreateHelperDateParameters(visitor);

        string tick = tickParameter.Name;
        string toSec = tickToSecondParameter.Name;
        long ticksPerDay = TimeSpan.TicksPerDay;
        int multiplier = addType == "years" ? 12 : 1;

        string objSql = obj.ToString();
        string srcDate = $"DATE((({objSql} - {tick}) / {toSec} - (CASE WHEN (({objSql} - {tick}) % {toSec}) < 0 THEN 1 ELSE 0 END)), 'unixepoch')";
        string day = $"CAST(STRFTIME('%d', {srcDate}) AS INTEGER)";

        string months;
        string monthsPlusOne;
        SQLiteParameter[] parameters;

        if (arguments[0].IsConstant)
        {
            long n = Convert.ToInt64(arguments[0].Constant) * multiplier;
            months = $"'{n} months'";
            monthsPlusOne = $"'{n + 1} months'";
            parameters = [.. obj.Parameters ?? [], tickParameter, tickToSecondParameter];
        }
        else
        {
            string nSql = arguments[0].SQLiteExpression!.ToString();
            months = $"((({nSql}) * {multiplier}) || ' months')";
            monthsPlusOne = $"((({nSql}) * {multiplier} + 1) || ' months')";
            parameters = [.. obj.Parameters ?? [], .. arguments[0].SQLiteExpression!.Parameters ?? [], tickParameter, tickToSecondParameter];
        }

        string overflowed = $"DATE({srcDate}, 'start of month', {months}, '+' || ({day} - 1) || ' days')";
        string lastDay = $"DATE({srcDate}, 'start of month', {monthsPlusOne}, '-1 day')";
        string clampedDate = $"MIN({overflowed}, {lastDay})";
        string clampedMidnight = $"(CAST(STRFTIME('%s', {clampedDate}) AS INTEGER) * {toSec} + {tick})";
        string sql = $"({clampedMidnight} + ({objSql} % {ticksPerDay}))";

        return SQLiteExpression.Leaf(method.ReturnType, visitor.Counters.NextIdentifier(), sql, parameters);
    }

    private static (SQLiteParameter TickParameter, SQLiteParameter TickToSecondParameter) CreateHelperDateParameters(SQLVisitor visitor)
    {
        SQLiteParameter tickParameter = new()
        {
            Name = visitor.Counters.NextParamName(),
            Value = 621355968000000000 // new DateTime(1970, 1, 1).Ticks
        };
        SQLiteParameter tickToSecondParameter = new()
        {
            Name = visitor.Counters.NextParamName(),
            Value = TimeSpan.TicksPerSecond
        };

        return (tickParameter, tickToSecondParameter);
    }

    private static SQLiteExpression DivModExpression(SQLVisitor visitor, Type type, SQLiteExpression node, long div, long mod)
    {
        return SQLiteExpression.Wrap(type, visitor.Counters.NextIdentifier(), "(", node, $" / {div}) % {mod}", node.Parameters);
    }

    private static SQLiteExpression DivExpression(SQLVisitor visitor, Type type, SQLiteExpression node, long div)
    {
        return SQLiteExpression.Wrap(type, visitor.Counters.NextIdentifier(), "CAST(", node, $" / {div} AS INTEGER)", node.Parameters);
    }

    private static SQLiteExpression DivAsRealExpression(SQLVisitor visitor, Type type, SQLiteExpression node, long div)
    {
        return SQLiteExpression.Wrap(type, visitor.Counters.NextIdentifier(), "(CAST(", node, $" AS REAL) / {div})", node.Parameters);
    }

    private static SQLiteExpression MulAsRealExpression(SQLVisitor visitor, Type type, SQLiteExpression node, long mul)
    {
        return SQLiteExpression.Wrap(type, visitor.Counters.NextIdentifier(), "(CAST(", node, $" AS REAL) * {mul})", node.Parameters);
    }

    private static SQLiteExpression ModMulExpression(SQLVisitor visitor, Type type, SQLiteExpression node, long mod, long mul)
    {
        return SQLiteExpression.Wrap(type, visitor.Counters.NextIdentifier(), "((", node, $" % {mod}) * {mul})", node.Parameters);
    }

    private static SQLiteExpression ModExpression(SQLVisitor visitor, Type type, SQLiteExpression node, long mod)
    {
        return SQLiteExpression.Wrap(type, visitor.Counters.NextIdentifier(), "(", node, $" % {mod})", node.Parameters);
    }

    private static SQLiteExpression DateTruncExpression(SQLVisitor visitor, Type type, SQLiteExpression node)
    {
        return SQLiteExpression.Wrap(type, visitor.Counters.NextIdentifier(), "((", node, $" / {TimeSpan.TicksPerDay}) * {TimeSpan.TicksPerDay})", node.Parameters);
    }

    private static SQLiteExpression ResolveDateFormat(SQLVisitor visitor, Type type, SQLiteExpression obj, string format, string function)
    {
        (SQLiteParameter tickParameter, SQLiteParameter tickToSecondParameter) = CreateHelperDateParameters(visitor);

        string tick = tickParameter.Name;
        string toSec = tickToSecondParameter.Name;

        return SQLiteExpression.Multi(
            type,
            visitor.Counters.NextIdentifier(),
            [
                $"CAST(STRFTIME('%{format}',{function}((",
                $" - {tick}) / {toSec} - (CASE WHEN ((",
                $" - {tick}) % {toSec}) < 0 THEN 1 ELSE 0 END), 'unixepoch')) AS INTEGER)"
            ],
            [obj, obj],
            [.. obj.Parameters ?? [], tickParameter, tickToSecondParameter]
        );
    }

    private static SQLiteExpression ResolveTimeFormat(SQLVisitor visitor, Type type, SQLiteExpression obj, string format)
    {
        SQLiteParameter tickToSecondParameter = new()
        {
            Name = visitor.Counters.NextParamName(),
            Value = TimeSpan.TicksPerSecond
        };
        return SQLiteExpression.Wrap(
            type,
            visitor.Counters.NextIdentifier(),
            $"CAST(STRFTIME('%{format}',TIME(",
            obj,
            $" / {tickToSecondParameter.Name}, 'unixepoch')) AS INTEGER)",
            [.. obj.Parameters ?? [], tickToSecondParameter]
        );
    }
}
