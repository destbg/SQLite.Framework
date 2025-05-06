using System.Collections;
using System.Linq.Expressions;
using System.Text;
using SQLite.Framework.Internals.Models;

namespace SQLite.Framework.Internals.Helpers;

internal class MethodHandler
{
    private readonly SQLVisitor visitor;

    public MethodHandler(SQLVisitor visitor)
    {
        this.visitor = visitor;
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

        throw new NotSupportedException($"Unsupported method {node.Method.Name} for String");
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

        throw new NotSupportedException($"Unsupported method {node.Method.Name} for Math");
    }

    public string HandleDateExtension(MethodCallExpression node)
    {
        string alias = visitor.Visit(node.Object!);

        switch (node.Method.Name)
        {
            case nameof(DateTime.Add):
            {
                return AppendDateAdd(node, alias, "seconds");
            }
            case nameof(DateTime.AddYears):
            {
                return AppendDateAdd(node, alias, "years");
            }
            case nameof(DateTime.AddDays):
            {
                return AppendDateAdd(node, alias, "days");
            }
            case nameof(DateTime.AddHours):
            {
                return AppendDateAdd(node, alias, "hours");
            }
            case nameof(DateTime.AddMinutes):
            {
                return AppendDateAdd(node, alias, "minutes");
            }
            case nameof(DateTime.AddSeconds):
            {
                return AppendDateAdd(node, alias, "seconds");
            }
            case nameof(DateTime.AddMilliseconds):
            {
                return AppendDateAdd(node, alias, "milliseconds");
            }
            case nameof(DateTime.AddTicks):
            {
                return AppendDateAdd(node, alias, "ticks");
            }
        }

        throw new NotSupportedException($"Unsupported method {node.Method.Name} for DateTime");
    }

    public string HandleGuidExtension(MethodCallExpression node)
    {
        string alias = visitor.Visit(node.Object!);

        switch (node.Method.Name)
        {
            case nameof(Guid.ToString):
            {
                return $"HEX({alias})";
            }
            case nameof(Guid.Equals):
            {
                string valueSql = visitor.Visit(node.Arguments[0]);
                return $"{alias} = {valueSql}";
            }
        }

        throw new NotSupportedException($"Unsupported method {node.Method.Name} for Guid");
    }

    public string HandleEnumerableExtension(MethodCallExpression node, IEnumerable enumerable)
    {
        if (node.Object == null && CommonHelpers.IsSimple(node.Method.ReturnType))
        {
            object? result = node.Method.Invoke(null, [
                enumerable,
                ..node.Arguments.Skip(1).Select(CommonHelpers.GetConstantValue)
            ]);
            string pName = $"@p{visitor.ParamIndex.Index++}";
            visitor.Parameters[pName] = result;

            return pName;
        }

        switch (node.Method.Name)
        {
            case nameof(Enumerable.Contains):
            {
                List<string> parameterNames = [];

                foreach (object obj in enumerable)
                {
                    string pName = $"@p{visitor.ParamIndex.Index++}";
                    visitor.Parameters[pName] = obj;
                    parameterNames.Add(pName);
                }

                string alias = visitor.Visit(node.Arguments[0]);

                return $"{alias} IN ({string.Join(", ", parameterNames)})";
            }
        }

        throw new NotSupportedException($"Unsupported method {node.Method.Name} for Enumerable");
    }

    public string HandleQueryableExtension(MethodCallExpression node)
    {
        if (node.Method.Name == nameof(Queryable.All))
        {
            throw new NotSupportedException($"Unsupported method {node.Method.Name} for Queryable");
        }

        MethodCallExpression nodeQueryable = (MethodCallExpression)node.Arguments[0];

        SQLTranslator translator = visitor.CloneDeeper(visitor.Level + 1);
        SQLQuery query = translator.Translate(nodeQueryable);

        if (node.Arguments.Count == 1)
        {
            if (node.Method.Name == nameof(Queryable.Any))
            {
                return $"EXISTS ({Environment.NewLine}{query.Sql}{Environment.NewLine})";
            }

            return $"{Environment.NewLine}{query.Sql}{Environment.NewLine}";
        }

        switch (node.Method.Name)
        {
            case nameof(Queryable.Contains):
            {
                string alias = visitor.Visit(node.Arguments[1]);
                return $"{alias} IN ({Environment.NewLine}{query.Sql}{Environment.NewLine})";
            }
        }

        throw new NotSupportedException($"Unsupported method {node.Method.Name} for Queryable");
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
            visitor.Parameters[pName] = selectParameter(value);
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

    private string AppendDateAdd(MethodCallExpression node, string alias, string addType)
    {
        if (CommonHelpers.IsConstant(node.Arguments[0]))
        {
            object? value = CommonHelpers.GetConstantValue(node.Arguments[0]);
            string pName = $"@p{visitor.ParamIndex.Index++}";
            visitor.Parameters[pName] = $"+{value} {addType}";
            return $"DATE({alias}, {pName})";
        }

        string valueSql = visitor.Visit(node.Arguments[0]);
        return $"DATE({alias}, '+'||{valueSql}||' {addType}')";
    }
}