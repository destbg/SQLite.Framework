using System.Linq.Expressions;
using System.Text;

namespace SQLite.Framework.Internals.Helpers;

internal class MethodHandler
{
    private readonly SQLVisitor visitor;
    private readonly Dictionary<string, object?> parameters;

    public MethodHandler(SQLVisitor visitor, Dictionary<string, object?> parameters)
    {
        this.visitor = visitor;
        this.parameters = parameters;
    }

    public string HandleStringExtension(MethodCallExpression node)
    {
        string alias = visitor.Visit(node.Object!);

        switch (node.Method.Name)
        {
            case nameof(string.Contains):
            {
                return AppendLike(node, alias, value => $"%{value}%", valueSql => $"'%'||{valueSql}||'%'");
            }
            case nameof(string.StartsWith):
            {
                return AppendLike(node, alias, value => $"{value}%", valueSql => $"{valueSql}||'%'");
            }
            case nameof(string.EndsWith):
            {
                return AppendLike(node, alias, value => $"%{value}", valueSql => $"'%'||{valueSql}");
            }
            case nameof(string.Equals):
            {
                string valueSql = visitor.Visit(node.Arguments[0]);
                return $"{alias} = {valueSql}";
            }
            case nameof(string.IndexOf):
            {
                string valueSql = visitor.Visit(node.Arguments[0]);
                return $"INSTR({alias}, {valueSql})";
            }
            case nameof(string.Replace):
            {
                string firstValueSql = visitor.Visit(node.Arguments[0]);
                string secondValueSql = visitor.Visit(node.Arguments[1]);
                return $"REPLACE({alias}, {firstValueSql}, {secondValueSql})";
            }
            case nameof(string.Trim):
            {
                return AppendTrim(node, alias, "TRIM");
            }
            case nameof(string.TrimStart):
            {
                return AppendTrim(node, alias, "LTRIM");
            }
            case nameof(string.TrimEnd):
            {
                return AppendTrim(node, alias, "RTRIM");
            }
            case nameof(string.Substring):
            {
                if (node.Arguments.Count == 2)
                {
                    string firstValueSql = visitor.Visit(node.Arguments[0]);
                    string secondValueSql = visitor.Visit(node.Arguments[1]);
                    return $"SUBSTR({alias}, {firstValueSql}, {secondValueSql})";
                }

                string valueSql = visitor.Visit(node.Arguments[0]);
                return $"SUBSTR({alias}, {valueSql})";
            }
            case nameof(string.ToUpper):
            case nameof(string.ToUpperInvariant):
            {
                return $"UPPER({alias})";
            }
            case nameof(string.ToLower):
            case nameof(string.ToLowerInvariant):
            {
                return $"LOWER({alias})";
            }
        }

        throw new NotSupportedException($"Unsupported method {node.Method.Name} for string");
    }

    public string HandleMathExtension(MethodCallExpression node)
    {
        string firstValue = visitor.Visit(node.Arguments[0]);

        switch (node.Method.Name)
        {
            case nameof(Math.Min):
            {
                string secondValue = visitor.Visit(node.Arguments[1]);
                return $"CASE WHEN {firstValue} > {secondValue} THEN {secondValue} ELSE {firstValue} END";
            }
            case nameof(Math.Max):
            {
                string secondValue = visitor.Visit(node.Arguments[1]);
                return $"CASE WHEN {firstValue} < {secondValue} THEN {secondValue} ELSE {firstValue} END";
            }
            case nameof(Math.Abs):
            {
                return $"ABS({firstValue})";
            }
            case nameof(Math.Round):
            {
                return $"ROUND({firstValue})";
            }
            case nameof(Math.Ceiling):
            {
                return $"CEIL({firstValue})";
            }
            case nameof(Math.Floor):
            {
                return $"FLOOR({firstValue})";
            }
        }

        throw new NotSupportedException($"Unsupported method {node.Method.Name} for math");
    }

    public string HandleDateExtension(MethodCallExpression node)
    {
        string alias = visitor.Visit(node.Object!);

        switch (node.Method.Name)
        {
            case nameof(DateTime.Add):
            {
                if (CommonHelpers.IsConstant(node.Arguments[0]))
                {
                    object? value = CommonHelpers.GetConstantValue(node.Arguments[0]);
                    string pName = $"@p{visitor.ParamIndex.Index++}";
                    parameters[pName] = $"+{value} seconds";
                    return $"DATE({alias}, {pName})";
                }

                string valueSql = visitor.Visit(node.Arguments[0]);
                return $"DATE({alias}, '+'||{valueSql}||' seconds')";
            }
            case nameof(DateTime.AddYears):
            {
                if (CommonHelpers.IsConstant(node.Arguments[0]))
                {
                    object? value = CommonHelpers.GetConstantValue(node.Arguments[0]);
                    string pName = $"@p{visitor.ParamIndex.Index++}";
                    parameters[pName] = $"+{value} years";
                    return $"DATE({alias}, {pName})";
                }

                string valueSql = visitor.Visit(node.Arguments[0]);
                return $"DATE({alias}, '+'||{valueSql}||' years')";
            }
            case nameof(DateTime.AddDays):
            {
                if (CommonHelpers.IsConstant(node.Arguments[0]))
                {
                    object? value = CommonHelpers.GetConstantValue(node.Arguments[0]);
                    string pName = $"@p{visitor.ParamIndex.Index++}";
                    parameters[pName] = $"+{value} days";
                    return $"DATE({alias}, {pName})";
                }

                string valueSql = visitor.Visit(node.Arguments[0]);
                return $"DATE({alias}, '+'||{valueSql}||' days')";
            }
            case nameof(DateTime.AddHours):
            {
                if (CommonHelpers.IsConstant(node.Arguments[0]))
                {
                    object? value = CommonHelpers.GetConstantValue(node.Arguments[0]);
                    string pName = $"@p{visitor.ParamIndex.Index++}";
                    parameters[pName] = $"+{value} hours";
                    return $"DATE({alias}, {pName})";
                }

                string valueSql = visitor.Visit(node.Arguments[0]);
                return $"DATE({alias}, '+'||{valueSql}||' hours')";
            }
            case nameof(DateTime.AddMinutes):
            {
                if (CommonHelpers.IsConstant(node.Arguments[0]))
                {
                    object? value = CommonHelpers.GetConstantValue(node.Arguments[0]);
                    string pName = $"@p{visitor.ParamIndex.Index++}";
                    parameters[pName] = $"+{value} minutes";
                    return $"DATE({alias}, {pName})";
                }

                string valueSql = visitor.Visit(node.Arguments[0]);
                return $"DATE({alias}, '+'||{valueSql}||' minutes')";
            }
            case nameof(DateTime.AddSeconds):
            {
                if (CommonHelpers.IsConstant(node.Arguments[0]))
                {
                    object? value = CommonHelpers.GetConstantValue(node.Arguments[0]);
                    string pName = $"@p{visitor.ParamIndex.Index++}";
                    parameters[pName] = $"+{value} seconds";
                    return $"DATE({alias}, {pName})";
                }

                string valueSql = visitor.Visit(node.Arguments[0]);
                return $"DATE({alias}, '+'||{valueSql}||' seconds')";
            }
            case nameof(DateTime.AddMilliseconds):
            {
                if (CommonHelpers.IsConstant(node.Arguments[0]))
                {
                    object? value = CommonHelpers.GetConstantValue(node.Arguments[0]);
                    string pName = $"@p{visitor.ParamIndex.Index++}";
                    parameters[pName] = $"+{value} milliseconds";
                    return $"DATE({alias}, {pName})";
                }

                string valueSql = visitor.Visit(node.Arguments[0]);
                return $"DATE({alias}, '+'||{valueSql}||' milliseconds')";
            }
            case nameof(DateTime.AddTicks):
            {
                if (CommonHelpers.IsConstant(node.Arguments[0]))
                {
                    object? value = CommonHelpers.GetConstantValue(node.Arguments[0]);
                    string pName = $"@p{visitor.ParamIndex.Index++}";
                    parameters[pName] = $"+{value} ticks";
                    return $"DATE({alias}, {pName})";
                }

                string valueSql = visitor.Visit(node.Arguments[0]);
                return $"DATE({alias}, '+'||{valueSql}||' ticks')";
            }
        }

        throw new NotSupportedException($"Unsupported method {node.Method.Name} for date");
    }

    private string AppendLike(MethodCallExpression node, string alias, Func<object?, string> selectParameter, Func<string, string> selectValue)
    {
        string noCase = string.Empty;
        if (node.Arguments.Count == 2)
        {
            StringComparison comparison = (StringComparison)CommonHelpers.GetConstantValue(node.Arguments[1])!;
            if (comparison is StringComparison.OrdinalIgnoreCase or StringComparison.CurrentCultureIgnoreCase or StringComparison.InvariantCultureIgnoreCase)
            {
                noCase = " COLLATE NOCASE";
            }
        }

        if (CommonHelpers.IsConstant(node.Arguments[0]))
        {
            object? value = CommonHelpers.GetConstantValue(node.Arguments[0]);
            string pName = $"@p{visitor.ParamIndex.Index++}";
            parameters[pName] = selectParameter(value);
            return $"{alias} LIKE {pName}{noCase}";
        }

        string valueSql = visitor.Visit(node.Arguments[0]);
        return $"{alias} LIKE {selectValue(valueSql)}{noCase}";
    }

    private string AppendTrim(MethodCallExpression node, string alias, string trimType)
    {
        if (node.Arguments.Count == 0)
        {
            return $"{trimType}({alias})";
        }

        if (node.Arguments[0] is NewArrayExpression expression)
        {
            string[] argumentsSql = expression.Expressions.Select(visitor.Visit).ToArray();
            StringBuilder sb = new($"{trimType}({alias}, {argumentsSql[0]})");

            for (int i = 1; i < argumentsSql.Length; i++)
            {
                sb.Insert(0, $"{trimType}(");
                sb.Append(", ");
                sb.Append(argumentsSql[i]);
                sb.Append(')');
            }

            return sb.ToString();
        }

        string valueSql = visitor.Visit(node.Arguments[0]);
        return $"{trimType}({alias}, {valueSql})";
    }
}