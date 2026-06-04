namespace SQLite.Framework.Internals.Visitors.SQL;

internal partial class SQLVisitor
{
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All entities have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "The type is an entity.")]
    protected override Expression VisitMember(MemberExpression node)
    {
        if (ExpressionHelpers.IsConstant(node))
        {
            object? value = ExpressionHelpers.GetConstantValue(node);
            if (value is SQLiteCte cte)
            {
                AssignCte(cte);
                return SQLiteExpression.Alias(node.Type, -1, From!, From!.Parameters);
            }
            else if (value is BaseSQLiteTable table)
            {
                AssignTable(table.ElementType);
                return SQLiteExpression.Alias(node.Type, -1, From!, From!.Parameters);
            }

            return SQLiteExpression.Leaf(node.Type, Counters.NextIdentifier(), Counters.NextParamName(), value);
        }

        Expression stripped = ExpressionHelpers.StripUpcast(node.Expression!);
        if (stripped != node.Expression)
        {
            node = node.Update(stripped);
        }

        if (node.Expression is not MemberExpression and not ParameterExpression)
        {
            node = (MemberExpression)ResolveMember(node);
        }

        if (node.Expression is MemberExpression or ParameterExpression or SQLiteExpression)
        {
            (string path, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(node);

            if (pe == null)
            {
                if (node.Expression is SQLiteExpression sqlExpression)
                {
                    return ConvertMemberExpression(node, sqlExpression);
                }

                return node.Update(Visit(node.Expression));
            }

            if (MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? expressions))
            {
                if (expressions.TryGetValue(path, out Expression? expression))
                {
                    if (expression is SQLiteExpression colExpr && !IsInSelectProjection)
                    {
                        Type colType = Nullable.GetUnderlyingType(colExpr.Type) ?? colExpr.Type;
                        if (colType == typeof(decimal) && Database.Options.DecimalStorage == DecimalStorageMode.Text)
                        {
                            return InternDecimalCast(colExpr);
                        }
                    }

                    return expression;
                }
            }

            (path, pe) = ExpressionHelpers.ResolveParameterPath(node.Expression);

            if (MethodArguments.TryGetValue(pe, out expressions))
            {
                if (expressions.TryGetValue(path, out Expression? expression) &&
                    expression is SQLiteExpression sqlExpression)
                {
                    return ConvertMemberExpression(node, sqlExpression);
                }

                if (path.Length == 0
                    && node.Expression is ParameterExpression
                    && expressions.Count == 1
                    && expressions.Values.Single() is SQLiteExpression singleExpression
                    && TypeHelpers.IsSimple(node.Expression.Type, Database.Options))
                {
                    return ConvertMemberExpression(node, singleExpression);
                }
            }
        }

        if (node.Expression is MemberExpression chainedExpression)
        {
            Expression visitedInner = Visit(chainedExpression);
            if (visitedInner is SQLiteExpression innerSqlExpression)
            {
                return ConvertMemberExpression(node, innerSqlExpression);
            }
        }

        return ResolveMember(node);
    }

    private Expression ConvertMemberExpression(MemberExpression node, SQLiteExpression sqlExpression)
    {
        if (Nullable.GetUnderlyingType(node.Expression!.Type) != null)
        {
            if (node.Member.Name == nameof(Nullable<int>.Value)
                && Nullable.GetUnderlyingType(node.Expression.Type) == typeof(decimal)
                && Database.Options.DecimalStorage == DecimalStorageMode.Text
                && !IsInSelectProjection)
            {
                return InternDecimalCast(sqlExpression);
            }

            return NullableMemberVisitor.HandleNullableProperty(this, node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(string))
        {
            return StringMemberVisitor.HandleStringProperty(this, node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(DateTime))
        {
            if (IsInSelectProjection && Level == 0 && Database.Options.DateTimeStorage == DateTimeStorageMode.TextFormatted)
            {
                return node.Update(sqlExpression);
            }

            return DateTimeMemberVisitor.HandleDateTimeProperty(this, node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(DateTimeOffset))
        {
            if (IsInSelectProjection && Level == 0 && Database.Options.DateTimeOffsetStorage == DateTimeOffsetStorageMode.TextFormatted)
            {
                return node.Update(sqlExpression);
            }

            return DateTimeMemberVisitor.HandleDateTimeOffsetProperty(this, node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(TimeSpan))
        {
            if (IsInSelectProjection && Level == 0 && Database.Options.TimeSpanStorage == TimeSpanStorageMode.Text)
            {
                return node.Update(sqlExpression);
            }

            return DateTimeMemberVisitor.HandleTimeSpanProperty(this, node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(DateOnly))
        {
            if (IsInSelectProjection && Level == 0 && Database.Options.DateOnlyStorage == DateOnlyStorageMode.Text)
            {
                return node.Update(sqlExpression);
            }

            return DateTimeMemberVisitor.HandleDateOnlyProperty(this, node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(TimeOnly))
        {
            if (IsInSelectProjection && Level == 0 && Database.Options.TimeOnlyStorage == TimeOnlyStorageMode.Text)
            {
                return node.Update(sqlExpression);
            }

            return DateTimeMemberVisitor.HandleTimeOnlyProperty(this, node.Member.Name, node.Type, sqlExpression);
        }

        string? translatedSql = Database.Options.TranslateProperty(node.Member.Name, sqlExpression.ToString());
        if (translatedSql != null)
        {
            return SQLiteExpression.Leaf(node.Type, Counters.NextIdentifier(), translatedSql, sqlExpression.Parameters);
        }

        if (Database.Options.HasJsonConverter(node.Expression.Type) || sqlExpression.IsJsonSource)
        {
            return InternJsonExtract(sqlExpression, node.Member.Name, node.Type);
        }

        if (Database.Options.HasTextOrBlobConverter(node.Expression.Type))
        {
            return node.Update(sqlExpression);
        }

        if (!TypeHelpers.IsSimple(node.Expression.Type, Database.Options)
            && Database.TableMappings.Any(m => m.Type == node.Expression.Type))
        {
            throw new NotSupportedException(
                $"Cannot read '{node.Member.Name}' from an entity-typed scalar subquery. " +
                $"Project the column inside the subquery first, e.g. " +
                $"'.Where(x => ...).Select(x => x.{node.Member.Name}).First()' " +
                $"instead of '.First(x => ...).{node.Member.Name}'.");
        }

        return sqlExpression;
    }
}
