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
    private HashSet<string> constructedPaths;
    private HashSet<string> optionalRowPaths;
    private Dictionary<string, Expression> constructedNodes;
    private bool carriesOptionalRow;

    public AliasVisitor(SQLiteDatabase database, SQLVisitor visitor)
    {
        this.database = database;
        this.visitor = visitor;
        result = [];
        resultPrefixes = [];
        constructedPaths = [];
        optionalRowPaths = [];
        constructedNodes = [];
    }

    public Dictionary<string, Expression> ResolveResultAlias(LambdaExpression resultSelector)
    {
        ResolveResultAlias(resultSelector, resultSelector.Body, string.Empty);
        if (resultSelector.Body is MemberInitExpression or NewExpression { Members: null, Arguments.Count: > 0 }
            && !database.TryGetCachedTableMapping(resultSelector.Body.Type, out _))
        {
            constructedNodes[string.Empty] = resultSelector.Body;
        }

        Dictionary<string, Expression> newResult = result;
        if (resultPrefixes.Count > 0)
        {
            visitor.TableColumnPrefixes[newResult] = resultPrefixes;
        }
        if (constructedPaths.Count > 0)
        {
            visitor.ConstructedProjectionPaths[newResult] = constructedPaths;
        }
        if (carriesOptionalRow)
        {
            visitor.OptionalRowColumns.Add(newResult);
        }
        if (optionalRowPaths.Count > 0)
        {
            visitor.OptionalRowPaths[newResult] = optionalRowPaths;
        }
        if (constructedNodes.Count > 0)
        {
            visitor.ConstructedProjectionNodes[newResult] = constructedNodes;
        }
        result = [];
        resultPrefixes = [];
        constructedPaths = [];
        optionalRowPaths = [];
        constructedNodes = [];
        carriesOptionalRow = false;
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

    private void CarryConstructedPaths(string prefix, Dictionary<string, Expression> sourceColumns)
    {
        if (visitor.ConstructedProjectionPaths.TryGetValue(sourceColumns, out HashSet<string>? sourceConstructed))
        {
            foreach (string path in sourceConstructed)
            {
                constructedPaths.Add(CheckPrefix(prefix, path));
            }
        }

        if (visitor.ConstructedProjectionNodes.TryGetValue(sourceColumns, out Dictionary<string, Expression>? sourceNodes))
        {
            foreach (KeyValuePair<string, Expression> node in sourceNodes)
            {
                constructedNodes[CheckPrefix(prefix, node.Key)] = node.Value;
            }
        }
    }

    private void CarryConstructedSubPaths(string alias, Dictionary<string, Expression> sourceColumns, string prefixToMatch)
    {
        if (visitor.ConstructedProjectionPaths.TryGetValue(sourceColumns, out HashSet<string>? sourceConstructed))
        {
            foreach (string path in sourceConstructed)
            {
                if (path.StartsWith(prefixToMatch, StringComparison.Ordinal))
                {
                    constructedPaths.Add(CheckPrefix(alias, path[prefixToMatch.Length..]));
                }
                else if (path == prefixToMatch[..^1])
                {
                    constructedPaths.Add(alias);
                }
            }
        }

        if (visitor.ConstructedProjectionNodes.TryGetValue(sourceColumns, out Dictionary<string, Expression>? sourceNodes))
        {
            foreach (KeyValuePair<string, Expression> node in sourceNodes)
            {
                if (node.Key.StartsWith(prefixToMatch, StringComparison.Ordinal))
                {
                    constructedNodes[CheckPrefix(alias, node.Key[prefixToMatch.Length..])] = node.Value;
                }
                else if (node.Key == prefixToMatch[..^1])
                {
                    constructedNodes[alias] = node.Value;
                }
            }
        }
    }

    private void ResolveResultAlias(LambdaExpression resultSelector, Expression body, string prefix)
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

    private void VisitNewExpression(LambdaExpression resultSelector, NewExpression newExpression, string prefix)
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
                        CarryConstructedPaths(alias, parameterTableColumns);
                        CarryOptionalRow(alias, parameterTableColumns);
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

                    CarryConstructedSubPaths(alias, sourceColumns, prefixToMatch);
                    CarryOptionalRowFromPath(alias, sourceColumns, path);
                }
                else if (argument is NewExpression or MemberInitExpression)
                {
                    string alias = CheckPrefix(prefix, parameter.Name!);
                    AliasVisitor nestedVisitor = new(database, visitor);
                    nestedVisitor.ResolveResultAlias(resultSelector, argument, alias);
                    foreach (KeyValuePair<string, Expression> tableColumn in nestedVisitor.result)
                    {
                        result.Add(tableColumn.Key, tableColumn.Value);
                    }

                    constructedPaths.Add(alias);
                    constructedPaths.UnionWith(nestedVisitor.constructedPaths);
                    optionalRowPaths.UnionWith(nestedVisitor.optionalRowPaths);

                    SQLVisitor innerVisitor = visitor.CloneForProjection(visitor.IsInSelectProjection);
                    Expression expression = innerVisitor.Visit(argument);

                    Expression node = CoalesceIfLiftedComparison(argument, expression);
                    result.Add(alias, node);
                    constructedNodes[alias] = node;
                }
                else
                {
                    string alias = CheckPrefix(prefix, parameter.Name!);
                    SQLVisitor innerVisitor = visitor.CloneForProjection(visitor.IsInSelectProjection);
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
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Projected nested types have a public parameterless constructor.")]
    private void VisitMemberInitExpression(LambdaExpression resultSelector, MemberInitExpression memberInitExpression, string prefix)
    {
        if (memberInitExpression.NewExpression.Arguments.Count > 0)
        {
            VisitNewExpression(resultSelector, memberInitExpression.NewExpression, prefix);
        }

        PropertyInfo[] declaredProperties = memberInitExpression.Type.GetProperties();
        IEnumerable<MemberBinding> orderedBindings = memberInitExpression.Bindings
            .OfType<MemberAssignment>()
            .Cast<MemberBinding>()
            .Concat(memberInitExpression.Bindings.OfType<MemberMemberBinding>())
            .Concat(memberInitExpression.Bindings.OfType<MemberListBinding>())
            .OrderBy(binding => Array.FindIndex(declaredProperties, p => p.Name == binding.Member.Name));

        foreach (MemberBinding binding in orderedBindings)
        {
            if (binding is MemberListBinding memberListBinding)
            {
                Type listType = memberListBinding.Member is PropertyInfo listProperty
                    ? listProperty.PropertyType
                    : ((FieldInfo)memberListBinding.Member).FieldType;
                string listAlias = CheckPrefix(prefix, memberListBinding.Member.Name);
                SQLVisitor listVisitor = visitor.CloneForProjection(visitor.IsInSelectProjection);
                Expression listNode = listVisitor.Visit(Expression.ListInit(Expression.New(listType), memberListBinding.Initializers));
                result[listAlias] = listNode;
                continue;
            }

            if (binding is MemberMemberBinding memberMemberBinding)
            {
                Type memberType = memberMemberBinding.Member is PropertyInfo memberProperty
                    ? memberProperty.PropertyType
                    : ((FieldInfo)memberMemberBinding.Member).FieldType;
                MemberInitExpression nested = Expression.MemberInit(Expression.New(memberType), memberMemberBinding.Bindings);
                string nestedAlias = CheckPrefix(prefix, memberMemberBinding.Member.Name);
                AliasVisitor nestedVisitor = new(database, visitor);
                nestedVisitor.ResolveResultAlias(resultSelector, nested, nestedAlias);
                foreach (KeyValuePair<string, Expression> tableColumn in nestedVisitor.result)
                {
                    result[tableColumn.Key] = tableColumn.Value;
                }

                constructedPaths.Add(nestedAlias);
                constructedPaths.UnionWith(nestedVisitor.constructedPaths);
                optionalRowPaths.UnionWith(nestedVisitor.optionalRowPaths);
                continue;
            }

            MemberAssignment memberAssignment = (MemberAssignment)binding;
            if (memberAssignment.Expression is MemberInitExpression or NewExpression)
            {
                string alias = CheckPrefix(prefix, memberAssignment.Member.Name);
                AliasVisitor innerVisitor = new(database, visitor);

                innerVisitor.ResolveResultAlias(resultSelector, memberAssignment.Expression, alias);
                Dictionary<string, Expression> innerResult = innerVisitor.result;

                foreach (KeyValuePair<string, Expression> tableColumn in innerResult)
                {
                    result[tableColumn.Key] = tableColumn.Value;
                }

                constructedPaths.Add(alias);
                constructedPaths.UnionWith(innerVisitor.constructedPaths);
                optionalRowPaths.UnionWith(innerVisitor.optionalRowPaths);

                SQLVisitor nestedNodeVisitor = visitor.CloneForProjection(visitor.IsInSelectProjection);
                Expression nestedNode = nestedNodeVisitor.Visit(memberAssignment.Expression);
                constructedNodes[alias] = CoalesceIfLiftedComparison(memberAssignment.Expression, nestedNode);
            }
            else if (memberAssignment.Expression is ParameterExpression parameterExpression)
            {
                string alias = CheckPrefix(prefix, memberAssignment.Member.Name);
                Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[parameterExpression];

                if (TypeHelpers.IsSimple(parameterExpression.Type, database.Options))
                {
                    result[alias] = parameterTableColumns.Values.First();
                }
                else
                {
                    foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                    {
                        result[$"{alias}.{tableColumn.Key}"] = tableColumn.Value;
                    }

                    CarrySubPaths(alias, parameterTableColumns);
                    CarryConstructedPaths(alias, parameterTableColumns);
                    CarryOptionalRow(alias, parameterTableColumns);
                }
            }
            else if (memberAssignment.Expression is MemberExpression)
            {
                string alias = CheckPrefix(prefix, memberAssignment.Member.Name);
                (string path, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(memberAssignment.Expression);

                if (pe == null)
                {
                    result[alias] = memberAssignment.Expression;
                    continue;
                }

                Dictionary<string, Expression> parameterTableColumns = visitor.MethodArguments[pe];

                if (TypeHelpers.IsSimple(memberAssignment.Expression.Type, database.Options))
                {
                    if (parameterTableColumns.TryGetValue(path, out Expression? columnExpression))
                    {
                        result[alias] = columnExpression;
                    }
                    else
                    {
                        SQLVisitor innerVisitor = visitor.CloneForProjection(visitor.IsInSelectProjection);
                        Expression expression = innerVisitor.Visit(memberAssignment.Expression);
                        result[alias] = CoalesceIfLiftedComparison(memberAssignment.Expression, expression);
                    }
                }
                else
                {
                    foreach (KeyValuePair<string, Expression> tableColumn in parameterTableColumns)
                    {
                        if (tableColumn.Key.StartsWith(path + "."))
                        {
                            result[$"{alias}.{tableColumn.Key[(path.Length + 1)..]}"] = tableColumn.Value;
                        }
                    }

                    CarryConstructedSubPaths(alias, parameterTableColumns, path + ".");
                    CarryOptionalRowFromPath(alias, parameterTableColumns, path);
                }
            }
            else
            {
                string alias = CheckPrefix(prefix, memberAssignment.Member.Name);
                Expression valueExpression = ExpressionHelpers.StripUpcast(memberAssignment.Expression);
                SQLVisitor innerVisitor = visitor.CloneForProjection(visitor.IsInSelectProjection);
                Expression expression = innerVisitor.Visit(valueExpression);
                result[alias] = CoalesceIfLiftedComparison(valueExpression, expression);
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

            CarryConstructedSubPaths(prefix, visitor.TableColumns, prefixToMatch);
            CarryOptionalRowFromPath(prefix, visitor.TableColumns, path);
        }
    }

    private void CarryOptionalRow(string alias, Dictionary<string, Expression> sourceColumns)
    {
        if (visitor.OptionalRowColumns.Contains(sourceColumns))
        {
            MarkOptionalRow(alias);
        }

        if (visitor.OptionalRowPaths.TryGetValue(sourceColumns, out HashSet<string>? sourceOptional))
        {
            foreach (string optionalPath in sourceOptional)
            {
                optionalRowPaths.Add(CheckPrefix(alias, optionalPath));
            }
        }
    }

    private void CarryOptionalRowFromPath(string alias, Dictionary<string, Expression> sourceColumns, string path)
    {
        if (!visitor.OptionalRowPaths.TryGetValue(sourceColumns, out HashSet<string>? sourceOptional))
        {
            return;
        }

        foreach (string optionalPath in sourceOptional)
        {
            if (optionalPath == path)
            {
                MarkOptionalRow(alias);
            }
            else if (optionalPath.StartsWith(path + ".", StringComparison.Ordinal))
            {
                optionalRowPaths.Add(CheckPrefix(alias, optionalPath[(path.Length + 1)..]));
            }
        }
    }

    private void MarkOptionalRow(string prefix)
    {
        if (prefix.Length == 0)
        {
            carriesOptionalRow = true;
        }
        else
        {
            optionalRowPaths.Add(prefix);
        }
    }

    private void VisitParameterExpression(ParameterExpression parameterExpression, string prefix)
    {
        Dictionary<string, Expression> tableColumns = visitor.MethodArguments[parameterExpression];

        foreach (KeyValuePair<string, Expression> tableColumn in tableColumns)
        {
            result.Add(CheckPrefix(prefix, tableColumn.Key), tableColumn.Value);
        }

        CarryConstructedPaths(prefix, tableColumns);
        CarryOptionalRow(prefix, tableColumns);
    }

    private void VisitMethodCallExpression(MethodCallExpression methodCallExpression, string prefix)
    {
        Expression expression = visitor.Visit(methodCallExpression);
        result.Add(prefix, expression);
    }

    private void VisitInnerExpression(Expression body, string prefix)
    {
        SQLVisitor innerVisitor = visitor.CloneForProjection(isInSelectProjection: false);
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
