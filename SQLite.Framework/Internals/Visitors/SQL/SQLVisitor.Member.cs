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
                return new SQLiteExpression(node.Type, -1, From!.Sql, From!.Parameters);
            }
            else if (value is BaseSQLiteTable table)
            {
                AssignTable(table.ElementType);
                return new SQLiteExpression(node.Type, -1, From!.Sql, From!.Parameters);
            }

            return new SQLiteExpression(node.Type, Counters.IdentifierIndex++, $"@p{Counters.ParamIndex++}", value);
        }

        if (node.Expression is UnaryExpression { NodeType: ExpressionType.Convert } cast
            && cast.Type.IsAssignableFrom(cast.Operand.Type))
        {
            node = node.Update(cast.Operand);
        }

        if (node.Expression is not MemberExpression and not ParameterExpression and not SQLiteExpression)
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
            }
        }

        return ResolveMember(node);
    }

    private Expression ConvertMemberExpression(MemberExpression node, SQLiteExpression sqlExpression)
    {
        if (Nullable.GetUnderlyingType(node.Expression!.Type) != null)
        {
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

        string? translatedSql = Database.Options.TranslateProperty(node.Member.Name, sqlExpression.Sql);
        if (translatedSql != null)
        {
            return new SQLiteExpression(node.Type, Counters.IdentifierIndex++, translatedSql, sqlExpression.Parameters);
        }

        if (Database.Options.HasJsonConverter(node.Expression.Type) || sqlExpression.IsJsonSource)
        {
            return InternJsonExtract(sqlExpression, node.Member.Name, node.Type);
        }

        if (Database.Options.HasTextOrBlobConverter(node.Expression.Type))
        {
            return node.Update(sqlExpression);
        }

        return sqlExpression;
    }
}
