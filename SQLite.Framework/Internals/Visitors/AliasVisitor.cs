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
    private Dictionary<string, string?> resultPrefixes;

    public AliasVisitor(SQLiteDatabase database, SQLVisitor visitor)
    {
        this.database = database;
        this.visitor = visitor;
        result = [];
        resultPrefixes = [];
    }

    public Dictionary<string, Expression> ResolveResultAlias(LambdaExpression resultSelector)
    {
        ResolveResultAlias(resultSelector, resultSelector.Body, string.Empty);
        Dictionary<string, Expression> newResult = result;
        if (resultPrefixes.Count > 0)
        {
            visitor.TableColumnPrefixes[newResult] = resultPrefixes;
        }
        result = [];
        resultPrefixes = [];
        return newResult;
    }

    private void CarrySubPaths(string alias, Dictionary<string, Expression> sourceColumns)
    {
        if (!visitor.TableColumnPrefixes.TryGetValue(sourceColumns, out Dictionary<string, string?>? sourcePrefixes))
        {
            return;
        }

        foreach (KeyValuePair<string, string?> sourcePrefix in sourcePrefixes)
        {
            string subPath = sourcePrefix.Key.Length == 0 ? alias : $"{alias}.{sourcePrefix.Key}";
            resultPrefixes[subPath] = sourcePrefix.Value;
        }
    }

    private void ResolveResultAlias(LambdaExpression resultSelector, Expression body, string prefix)
    {
        switch (body)
        {
            case NewExpression ne:
                VisitNewExpression(ne, prefix);
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

    private void VisitNewExpression(NewExpression newExpression, string prefix)
    {
        if (newExpression.Arguments.Count > 0)
        {
            ConstructorInfo ctor = newExpression.Constructor!;
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
                    string alias = CheckPrefix(prefix, parameter.Name!);
                    Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[parameterExpression];

                    if (TypeHelpers.IsSimple(parameterExpression.Type, database.Options))
                    {
                        result.Add(alias, parameterTableColumns.Values.First());
                    }
                    else
                    {
                        foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                        {
                            result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                        }

                        CarrySubPaths(alias, parameterTableColumns);
                    }
                }
                else if (argument is MemberExpression memberExpression
                    && !TypeHelpers.IsSimple(memberExpression.Type, database.Options))
                {
                    string alias = CheckPrefix(prefix, parameter.Name!);
                    (string path, ParameterExpression rangeParameter) = ExpressionHelpers.ResolveParameterPath(memberExpression);
                    Dictionary<string, Expression> sourceColumns = visitor.MethodArguments[rangeParameter];
                    string prefixToMatch = path + ".";

                    foreach (KeyValuePair<string, Expression> tableColumn in sourceColumns)
                    {
                        if (tableColumn.Key.StartsWith(prefixToMatch, StringComparison.Ordinal))
                        {
                            string suffix = tableColumn.Key[prefixToMatch.Length..];
                            result.Add($"{alias}.{suffix}", tableColumn.Value);
                        }
                    }
                }
                else
                {
                    string alias = CheckPrefix(prefix, parameter.Name!);
                    SQLVisitor innerVisitor = new(database, visitor.Counters, visitor.Level + 1)
                    {
                        MethodArguments = visitor.MethodArguments,
                        TableColumnPrefixes = visitor.TableColumnPrefixes,
                        ClientEvalAllowed = visitor.ClientEvalAllowed
                    };
                    Expression expression = innerVisitor.Visit(argument);

                    result.Add(alias, CoalesceIfLiftedComparison(argument, expression));
                }
            }
        }
        else if (newExpression.Members == null)
        {
            throw new NotSupportedException(
                $"Cannot translate Select projection 'new {newExpression.Type.Name}()': " +
                "use a constructor with arguments or a member-initializer (e.g., 'new T { Prop = value }').");
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "All types have public properties.")]
    private void VisitMemberInitExpression(LambdaExpression resultSelector, MemberInitExpression memberInitExpression, string prefix)
    {
        if (memberInitExpression.NewExpression.Arguments.Count > 0)
        {
            VisitNewExpression(memberInitExpression.NewExpression, prefix);
        }

        PropertyInfo[] declaredProperties = memberInitExpression.Type.GetProperties();
        IEnumerable<MemberAssignment> orderedBindings = memberInitExpression.Bindings
            .OfType<MemberAssignment>()
            .OrderBy(binding => Array.FindIndex(declaredProperties, p => p.Name == binding.Member.Name));

        foreach (MemberAssignment memberAssignment in orderedBindings)
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

                if (TypeHelpers.IsSimple(parameterExpression.Type, database.Options))
                {
                    result.Add(alias, parameterTableColumns.Values.First());
                }
                else
                {
                    foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                    {
                        result.Add($"{alias}.{tableColumn.Key}", tableColumn.Value);
                    }

                    CarrySubPaths(alias, parameterTableColumns);
                }
            }
            else if (memberAssignment.Expression is MemberExpression)
            {
                string alias = CheckPrefix(prefix, memberAssignment.Member.Name);
                (string path, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(memberAssignment.Expression);

                if (pe == null)
                {
                    result.Add(alias, memberAssignment.Expression);
                    continue;
                }

                Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[pe];

                if (TypeHelpers.IsSimple(memberAssignment.Expression.Type, database.Options))
                {
                    result.Add(alias, parameterTableColumns[path]);
                }
                else
                {
                    foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                    {
                        if (tableColumn.Key.StartsWith(path + "."))
                        {
                            result.Add($"{alias}.{tableColumn.Key[(path.Length + 1)..]}", tableColumn.Value);
                        }
                    }
                }
            }
            else
            {
                string alias = CheckPrefix(prefix, memberAssignment.Member.Name);
                SQLVisitor innerVisitor = new(database, visitor.Counters, visitor.Level + 1)
                {
                    MethodArguments = visitor.MethodArguments,
                    TableColumnPrefixes = visitor.TableColumnPrefixes,
                    ClientEvalAllowed = visitor.ClientEvalAllowed
                };
                Expression expression = innerVisitor.Visit(memberAssignment.Expression);
                result.Add(alias, CoalesceIfLiftedComparison(memberAssignment.Expression, expression));
            }
        }
    }

    private void VisitMemberExpression(MemberExpression memberExpression, string prefix)
    {
        if (TypeHelpers.IsSimple(memberExpression.Type, database.Options))
        {
            Expression columnMapping = visitor.Visit(memberExpression);
            result.Add(CheckPrefix(prefix, memberExpression.Member.Name), columnMapping);
        }
        else
        {
            (string path, ParameterExpression _) = ExpressionHelpers.ResolveParameterPath(memberExpression);
            string prefixToMatch = path + ".";

            foreach (KeyValuePair<string, Expression> tableColumn in visitor.TableColumns)
            {
                if (tableColumn.Key.StartsWith(prefixToMatch, StringComparison.Ordinal))
                {
                    string newPath = tableColumn.Key[prefixToMatch.Length..];
                    result.Add(CheckPrefix(prefix, newPath), tableColumn.Value);
                }
            }
        }
    }

    private void VisitParameterExpression(ParameterExpression parameterExpression, string prefix)
    {
        Dictionary<string, Expression> tableColumns = visitor.MethodArguments[parameterExpression];

        foreach (KeyValuePair<string, Expression> tableColumn in tableColumns)
        {
            result.Add(CheckPrefix(prefix, tableColumn.Key), tableColumn.Value);
        }
    }

    private void VisitMethodCallExpression(MethodCallExpression methodCallExpression, string prefix)
    {
        Expression expression = visitor.Visit(methodCallExpression);
        result.Add(prefix, expression);
    }

    private void VisitInnerExpression(Expression body, string prefix)
    {
        SQLVisitor innerVisitor = new(database, visitor.Counters, visitor.Level + 1)
        {
            MethodArguments = visitor.MethodArguments,
            TableColumnPrefixes = visitor.TableColumnPrefixes,
            ClientEvalAllowed = visitor.ClientEvalAllowed
        };
        Expression expression = innerVisitor.Visit(body);
        result.Add(prefix, CoalesceIfLiftedComparison(body, expression));
    }

    private Expression CoalesceIfLiftedComparison(Expression source, Expression resolved)
    {
        return resolved is SQLiteExpression sqlExpr
            ? visitor.CoalesceLiftedOrderComparison(source, sqlExpr)
            : resolved;
    }

    private static string CheckPrefix(string prefix, string path)
    {
        return prefix.Length > 0 ? $"{prefix}.{path}" : path;
    }
}
