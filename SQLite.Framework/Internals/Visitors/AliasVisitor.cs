using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;

namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// AliasVisitor is a class that goes through result selectors
/// and finds all references to columns in the result set.
/// </summary>
internal class AliasVisitor
{
    private readonly SQLiteDatabase database;
    private readonly SQLVisitor visitor;
    private Dictionary<string, Expression> result;

    public AliasVisitor(SQLiteDatabase database, SQLVisitor visitor)
    {
        this.database = database;
        this.visitor = visitor;
        result = [];
    }

    public Dictionary<string, Expression> ResolveResultAlias(LambdaExpression resultSelector)
    {
        ResolveResultAlias(resultSelector, resultSelector.Body, null);
        Dictionary<string, Expression> newResult = result;
        result = [];
        return newResult;
    }

    private void ResolveResultAlias(LambdaExpression resultSelector, Expression body, string? prefix)
    {
        switch (body)
        {
            case NewExpression ne:
                VisitNewExpression(resultSelector, ne, prefix);
                break;
            case MemberInitExpression mie:
                VisitMemberInitExpression(resultSelector, mie, prefix);
                break;
            case MemberExpression me:
                VisitMemberExpression(me, prefix);
                break;
            case ParameterExpression pe:
                VisitParameterExpression(pe, prefix);
                break;
            case MethodCallExpression mce:
                VisitMethodCallExpression(mce, prefix);
                break;
            default:
                VisitInnerExpression(body, prefix);
                break;
        }
    }

    private void VisitNewExpression(LambdaExpression resultSelector, NewExpression newExpression, string? prefix)
    {
        if (newExpression.Arguments.Count > 0)
        {
            ConstructorInfo ctor = newExpression.Constructor ?? throw new NotSupportedException("Cannot translate new expression without constructor");
            ParameterInfo[] parameters = ctor.GetParameters();

            if (parameters.Length != newExpression.Arguments.Count)
            {
                throw new NotSupportedException($"Constructor {ctor.Name} has {parameters.Length} parameters, but {newExpression.Arguments.Count} arguments were provided.");
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                Expression argument = newExpression.Arguments[i];
                ParameterInfo parameter = parameters[i];

                if (argument is ParameterExpression parameterExpression)
                {
                    string alias = CheckPrefix(prefix, parameter.Name ?? parameterExpression.Name!);
                    Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[parameterExpression];

                    foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                    {
                        result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                    }
                }
                else if (argument is MemberExpression memberExpression)
                {
                    string alias = CheckPrefix(prefix, parameter.Name ?? memberExpression.Member.Name);
                    (string path, ParameterExpression? pe) = CommonHelpers.ResolveNullableParameterPath(memberExpression);

                    if (pe == null)
                    {
                        result.Add(alias, memberExpression);
                        continue;
                    }

                    Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[pe];

                    if (CommonHelpers.IsSimple(memberExpression.Type))
                    {
                        result.Add(alias, parameterTableColumns[path]);
                    }
                    else
                    {
                        foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                        {
                            if (tableColumn.Key.StartsWith(path))
                            {
                                result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                            }
                        }
                    }
                }
                else if (argument is MethodCallExpression { Arguments.Count: 1 } methodCallExpression && methodCallExpression.Arguments[0].Type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
                {
                    string alias = CheckPrefix(prefix, parameter.Name ?? methodCallExpression.Method.Name);

                    Expression expression = visitor.MethodVisitor.HandleGroupingMethod(methodCallExpression);

                    if (expression is SQLExpression sqlExpression)
                    {
                        result.Add(alias, sqlExpression);
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
                string alias = CheckPrefix(prefix, memberInfo.Name);
                Type propertyType = memberInfo is PropertyInfo pi ? pi.PropertyType : ((FieldInfo)memberInfo).FieldType;

                ParameterExpression expression = resultSelector.Parameters
                    .First(f => (f.Name == memberInfo.Name && f.Type == propertyType) || f.Type == propertyType);

                (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(expression);

                Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[pe];

                foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                {
                    if (tableColumn.Key.StartsWith(path))
                    {
                        result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                    }
                }
            }
        }
    }

    private void VisitMemberInitExpression(LambdaExpression resultSelector, MemberInitExpression memberInitExpression, string? prefix)
    {
        foreach (MemberAssignment memberAssignment in memberInitExpression.Bindings.Cast<MemberAssignment>())
        {
            if (memberAssignment.Expression is MemberInitExpression or NewExpression)
            {
                string alias = CheckPrefix(prefix, memberAssignment.Member.Name);
                AliasVisitor innerVisitor = new(database, visitor);

                innerVisitor.ResolveResultAlias(resultSelector, memberAssignment.Expression, alias);
                Dictionary<string, Expression> innerResult = innerVisitor.result;

                foreach (KeyValuePair<string, Expression> tableColumn in innerResult)
                {
                    result.Add(tableColumn.Key, tableColumn.Value);
                }
            }
            else if (memberAssignment.Expression is ParameterExpression parameterExpression)
            {
                string alias = CheckPrefix(prefix, memberAssignment.Member.Name);
                Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[parameterExpression];

                foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                {
                    result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                }
            }
            else if (memberAssignment.Expression is MemberExpression)
            {
                string alias = CheckPrefix(prefix, memberAssignment.Member.Name);
                (string path, ParameterExpression? pe) = CommonHelpers.ResolveNullableParameterPath(memberAssignment.Expression);

                if (pe == null)
                {
                    result.Add(alias, memberAssignment.Expression);
                    continue;
                }

                Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[pe];

                if (CommonHelpers.IsSimple(memberAssignment.Expression.Type))
                {
                    result.Add(alias, parameterTableColumns[path]);
                }
                else
                {
                    foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
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
                string alias = CheckPrefix(prefix, memberAssignment.Member.Name);
                SQLVisitor innerVisitor = new(database, visitor.ParamIndex, visitor.TableIndex, visitor.Level + 1)
                {
                    MethodArguments = visitor.MethodArguments
                };
                Expression expression = innerVisitor.Visit(memberAssignment.Expression);
                result.Add(alias, expression);
            }
        }
    }

    private void VisitMemberExpression(MemberExpression memberExpression, string? prefix)
    {
        if (CommonHelpers.IsSimple(memberExpression.Type))
        {
            Expression columnMapping = visitor.Visit(memberExpression);
            result.Add(CheckPrefix(prefix, memberExpression.Member.Name), columnMapping);
        }
        else
        {
            (string path, ParameterExpression _) = CommonHelpers.ResolveParameterPath(memberExpression);

            foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
            {
                if (tableColumn.Key.StartsWith(path))
                {
                    string newPath = tableColumn.Key[(path.Length + 1)..];
                    result.Add(CheckPrefix(prefix, newPath), tableColumn.Value);
                }
            }
        }
    }

    private void VisitParameterExpression(ParameterExpression parameterExpression, string? prefix)
    {
        Dictionary<string, Expression> tableColumns = visitor.MethodArguments[parameterExpression];

        foreach (KeyValuePair<string, Expression> tableColumn in tableColumns)
        {
            result.Add(CheckPrefix(prefix, tableColumn.Key), tableColumn.Value);
        }
    }

    private void VisitMethodCallExpression(MethodCallExpression methodCallExpression, string? prefix)
    {
        Expression sql = visitor.Visit(methodCallExpression);

        if (sql is not SQLExpression sqlExpression)
        {
            throw new NotSupportedException($"Unsupported expression {methodCallExpression}");
        }

        result.Add(prefix ?? string.Empty, sqlExpression);
    }

    private void VisitInnerExpression(Expression body, string? prefix)
    {
        SQLVisitor innerVisitor = new(database, visitor.ParamIndex, visitor.TableIndex, visitor.Level + 1)
        {
            MethodArguments = visitor.MethodArguments
        };
        Expression sql = innerVisitor.Visit(body);

        if (sql is not SQLExpression sqlExpression)
        {
            throw new NotSupportedException($"Unsupported expression {body}");
        }

        result.Add(prefix ?? string.Empty, sqlExpression);
    }

    private static string CheckPrefix(string? prefix, string path)
    {
        return prefix != null ? $"{prefix}.{path}" : path;
    }
}