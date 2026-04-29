namespace SQLite.Framework.Internals.JSON;

internal partial class JsonCollectionVisitor
{
    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private void ProcessMethod(MethodCallExpression call, Type sourceType)
    {
        Type elementType = TypeHelpers.GetEnumerableElementType(sourceType) ?? typeof(object);

        switch (call.Method.Name)
        {
            case nameof(Enumerable.Where):
            {
                string predSql = VisitLambda(call.Arguments[1], elementType);
                wheres.Add(predSql);
                break;
            }
            case nameof(Enumerable.OrderBy):
            {
                orderBys.Clear();
                string keySql = VisitLambda(call.Arguments[1], elementType);
                orderBys.Add($"{keySql} ASC");
                break;
            }
            case nameof(Enumerable.OrderByDescending):
            {
                orderBys.Clear();
                string keySql = VisitLambda(call.Arguments[1], elementType);
                orderBys.Add($"{keySql} DESC");
                break;
            }
            case nameof(Enumerable.ThenBy):
            {
                string keySql = VisitLambda(call.Arguments[1], elementType);
                orderBys.Add($"{keySql} ASC");
                break;
            }
            case nameof(Enumerable.ThenByDescending):
            {
                string keySql = VisitLambda(call.Arguments[1], elementType);
                orderBys.Add($"{keySql} DESC");
                break;
            }
            case nameof(Enumerable.GroupBy):
            {
                string keySql = VisitLambda(call.Arguments[1], elementType);
                groupBys.Add(keySql);
                break;
            }
            case nameof(Enumerable.Select):
            {
                string projSql = VisitLambda(call.Arguments[1], elementType);
                selectExpr = projSql;
                break;
            }
            case nameof(Enumerable.SelectMany):
            {
                string selSql = VisitLambdaAliased(call.Arguments[1], elementType, "e");
                crossJoin = $", json_each({selSql}) n";
                selectExpr = "n.value";
                break;
            }
            case nameof(Enumerable.Skip):
            {
                ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
                offset = arg.SQLiteExpression?.Sql ?? arg.Constant?.ToString() ?? "0";
                AddParameters(arg);
                break;
            }
            case nameof(Enumerable.Take):
            {
                ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
                limit = arg.SQLiteExpression?.Sql ?? arg.Constant?.ToString() ?? "0";
                AddParameters(arg);
                break;
            }
            case nameof(Enumerable.First) or nameof(Enumerable.FirstOrDefault):
            {
                if (call.Arguments.Count > 1)
                {
                    string predSql = VisitLambda(call.Arguments[1], elementType);
                    wheres.Add(predSql);
                }

                limit = "1";
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Last) or nameof(Enumerable.LastOrDefault):
            {
                if (call.Arguments.Count > 1)
                {
                    string predSql = VisitLambda(call.Arguments[1], elementType);
                    wheres.Add(predSql);
                }

                if (orderBys.Count == 0)
                {
                    orderBys.Add("key DESC");
                }
                else
                {
                    for (int i = 0; i < orderBys.Count; i++)
                    {
                        orderBys[i] = orderBys[i].EndsWith(" ASC")
                            ? orderBys[i][..^4] + " DESC"
                            : orderBys[i][..^5] + " ASC";
                    }
                }

                limit = "1";
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Single) or nameof(Enumerable.SingleOrDefault):
            {
                if (call.Arguments.Count > 1)
                {
                    string predSql = VisitLambda(call.Arguments[1], elementType);
                    wheres.Add(predSql);
                }

                singleSemantic = true;
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Count):
            {
                if (call.Arguments.Count > 1)
                {
                    string predSql = VisitLambda(call.Arguments[1], elementType);
                    wheres.Add(predSql);
                }

                selectExpr = "COUNT(*)";
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Any):
            {
                if (call.Arguments.Count > 1)
                {
                    string predSql = VisitLambda(call.Arguments[1], elementType);
                    wheres.Add(predSql);
                }

                existsWrapper = "EXISTS";
                selectExpr = "1";
                limit = "1";
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.All):
            {
                string predSql = VisitLambda(call.Arguments[1], elementType);
                wheres.Add($"NOT ({predSql})");
                existsWrapper = "NOT EXISTS";
                selectExpr = "1";
                limit = "1";
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Min):
            {
                if (call.Arguments.Count > 1)
                {
                    string selSql = VisitLambda(call.Arguments[1], elementType);
                    selectExpr = $"MIN({selSql})";
                }
                else
                {
                    selectExpr = "MIN(value)";
                }

                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Max):
            {
                if (call.Arguments.Count > 1)
                {
                    string selSql = VisitLambda(call.Arguments[1], elementType);
                    selectExpr = $"MAX({selSql})";
                }
                else
                {
                    selectExpr = "MAX(value)";
                }

                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Sum):
            {
                if (call.Arguments.Count > 1)
                {
                    string selSql = VisitLambda(call.Arguments[1], elementType);
                    selectExpr = $"SUM({selSql})";
                }
                else
                {
                    selectExpr = "SUM(value)";
                }

                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Average):
            {
                if (call.Arguments.Count > 1)
                {
                    string selSql = VisitLambda(call.Arguments[1], elementType);
                    selectExpr = $"AVG({selSql})";
                }
                else
                {
                    selectExpr = "AVG(value)";
                }

                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Distinct):
            {
                distinct = true;
                break;
            }
            case nameof(Enumerable.Reverse):
            {
                if (orderBys.Count == 0)
                {
                    orderBys.Add("key DESC");
                }
                else
                {
                    for (int i = 0; i < orderBys.Count; i++)
                    {
                        orderBys[i] = orderBys[i].EndsWith(" ASC")
                            ? orderBys[i][..^4] + " DESC"
                            : orderBys[i][..^5] + " ASC";
                    }
                }

                break;
            }
            case nameof(Enumerable.ElementAt):
            {
                ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
                string idxSql = arg.SQLiteExpression?.Sql ?? arg.Constant?.ToString() ?? "0";
                AddParameters(arg);
                limit = "1";
                offset = idxSql;
                wrapInArray = false;
                break;
            }
            case nameof(Enumerable.Contains):
            {
                ResolvedModel arg = visitor.ResolveExpression(call.Arguments[1]);
                string valSql = arg.SQLiteExpression?.Sql ?? arg.Constant?.ToString() ?? "NULL";
                AddParameters(arg);
                wheres.Add($"value = {valSql}");
                existsWrapper = "EXISTS";
                selectExpr = "1";
                limit = "1";
                wrapInArray = false;
                break;
            }
        }
    }
}
