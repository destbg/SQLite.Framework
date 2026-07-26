namespace SQLite.Framework.Internals.JSON;

internal partial class JsonCollectionVisitor
{
    private string ArrayElementExpr(string column)
    {
        if (currentElementType == typeof(bool) || currentElementType == typeof(bool?))
        {
            return $"(CASE WHEN {column} IS NULL THEN NULL WHEN {column} THEN json('true') ELSE json('false') END)";
        }

        if (!TypeHelpers.IsSimple(currentElementType, options) || options.HasJsonConverter(currentElementType))
        {
            return $"json({column})";
        }

        return column;
    }

    private string EnsureInnerReference(string expr)
    {
        string bare = WithoutQuotedText(expr);
        foreach (string alias in innerAliases)
        {
            if (bare.Contains(alias + ".", StringComparison.Ordinal))
            {
                return expr;
            }
        }

        return $"(CASE WHEN {keyColumn} IS NOT NULL THEN {expr} END)";
    }

    private static string WithoutQuotedText(string expr)
    {
        StringBuilder builder = StringBuilderPool.Rent();
        bool inQuotedName = false;
        bool inLiteral = false;
        foreach (char c in expr)
        {
            if (c == '"' && !inLiteral)
            {
                inQuotedName = !inQuotedName;
            }
            else if (c == '\'' && !inQuotedName)
            {
                inLiteral = !inLiteral;
            }
            else if (!inQuotedName && !inLiteral)
            {
                builder.Append(c);
            }
        }

        return StringBuilderPool.ToStringAndReturn(builder);
    }

    private string BuildSql(string sourceSql)
    {
        string sp = new(' ', (visitor.Level + 1) * 4);
        string sp2 = new(' ', (visitor.Level + 2) * 4);
        string nl = Environment.NewLine;

        string distinctKeyword = distinct ? "DISTINCT " : "";
        string joinClause = crossJoin ?? "";
        string fromClause = fromOverride ?? $"json_each({sourceSql}) {baseAlias}{joinClause}";

        List<string> clauses = [$"SELECT {distinctKeyword}{selectExpr}", $"FROM {fromClause}"];

        if (wheres.Count > 0)
        {
            clauses.Add("WHERE " + string.Join(" AND ", wheres));
        }

        if (groupBys.Count > 0)
        {
            clauses.Add("GROUP BY " + string.Join(", ", groupBys));
        }

        if (havings.Count > 0)
        {
            clauses.Add("HAVING " + string.Join(" AND ", havings));
        }

        if (orderBys.Count > 0)
        {
            clauses.Add("ORDER BY " + string.Join(", ", orderBys));
        }

        string? limitOffset = LimitOffsetClause();
        if (limitOffset != null)
        {
            clauses.Add(limitOffset);
        }

        string innerSelect = string.Join(nl + sp, clauses);

        if (existsWrapper != null)
        {
            return $"{existsWrapper} ({nl}{sp}{innerSelect}{nl}{sp})";
        }

        if (singleSemantic)
        {
            string countSelect;
            if (groupBys.Count > 0)
            {
                List<string> groupedCountClauses = [.. clauses];
                groupedCountClauses[0] = $"SELECT {distinctKeyword}{selectExpr} AS \"value\"";
                groupedCountClauses.Add("LIMIT 2");
                string groupedCountInner = string.Join(nl + sp2, groupedCountClauses);
                countSelect = $"SELECT COUNT(*){nl}{sp2}FROM ({nl}{sp2}{groupedCountInner}{nl}{sp2})";
            }
            else
            {
                List<string> countClauses = [.. clauses];
                countClauses[0] = distinct ? $"SELECT COUNT(DISTINCT {EnsureInnerReference(selectExpr)})" : "SELECT COUNT(*)";
                countClauses.Add("LIMIT 2");
                countSelect = string.Join(nl + sp2, countClauses);
            }

            List<string> valueClauses = [.. clauses];
            valueClauses.Add("LIMIT 1");
            string valueSelect = string.Join(nl + sp2, valueClauses);

            return $"(CASE WHEN ({nl}{sp2}{countSelect}{nl}{sp}) = 1 THEN ({nl}{sp2}{valueSelect}{nl}{sp}) ELSE NULL END)";
        }

        if (wrapInArray)
        {
            if (groupBys.Count > 0)
            {
                List<string> groupedClauses = [.. clauses];
                groupedClauses[0] = $"SELECT {distinctKeyword}{selectExpr} AS \"value\"";
                string groupedInner = string.Join(nl + sp2, groupedClauses);
                return $"({nl}{sp}SELECT json_group_array({ArrayElementExpr("\"value\"")}){nl}{sp}FROM ({nl}{sp2}{groupedInner}{nl}{sp}){nl})";
            }

            if (distinct && (reverseApplied || distinctSeenReverse))
            {
                string positionAggregate = distinctSeenReverse ? "MAX" : "MIN";
                List<(string Expr, string Direction)> comboPending = SplitOrderBys().Where(p => p.Expr != keyColumn).ToList();
                List<string> comboClauses = [$"SELECT {selectExpr} AS \"value\"", $"FROM {fromClause}"];
                if (wheres.Count > 0)
                {
                    comboClauses.Add("WHERE " + string.Join(" AND ", wheres));
                }

                comboClauses.Add($"GROUP BY {selectExpr}");
                List<string> comboOrder = [];
                foreach ((string expr, string direction) in comboPending)
                {
                    string operand = EnsureInnerReference(expr);
                    comboOrder.Add(direction == "DESC" ? $"MAX({operand}) DESC" : $"MIN({operand}) ASC");
                }

                comboOrder.Add($"{positionAggregate}({keyColumn}) {(reverseApplied ? "DESC" : "ASC")}");
                comboClauses.Add("ORDER BY " + string.Join(", ", comboOrder));
                string comboInner = string.Join(nl + sp2, comboClauses);
                return $"({nl}{sp}SELECT json_group_array({ArrayElementExpr("\"value\"")}){nl}{sp}FROM ({nl}{sp2}{comboInner}{nl}{sp}){nl})";
            }

            bool needsSubquery = orderBys.Count > 0 || limit != null || offset != null;
            if (needsSubquery)
            {
                bool projectionIsValueColumn = selectExpr.EndsWith("\"value\"");
                string arrayColumn = projectionIsValueColumn ? "\"value\"" : "\"item\"";
                List<string> arrayClauses = [.. clauses];
                if (!projectionIsValueColumn)
                {
                    arrayClauses[0] = $"SELECT {distinctKeyword}{selectExpr} AS {arrayColumn}";
                }

                if (orderBys.Count > 0 && limit == null && offset == null && fromOverride != null)
                {
                    arrayClauses.Add("LIMIT -1");
                }

                string innerSelect2 = string.Join(nl + sp2, arrayClauses);
                return $"({nl}{sp}SELECT json_group_array({(distinct ? "DISTINCT " : "")}{ArrayElementExpr(arrayColumn)}){nl}{sp}FROM ({nl}{sp2}{innerSelect2}{nl}{sp}){nl})";
            }

            string aggregatedElement = EnsureInnerReference(ArrayElementExpr(selectExpr));

            clauses[0] = $"SELECT json_group_array({distinctKeyword}{aggregatedElement})";
            string simpleSelect = string.Join(nl + sp, clauses);
            return $"({nl}{sp}{simpleSelect}{nl})";
        }

        if (countsGroups)
        {
            List<string> groupedClauses = ["SELECT 1 AS \"value\"", $"FROM {fromClause}"];
            if (wheres.Count > 0)
            {
                groupedClauses.Add("WHERE " + string.Join(" AND ", wheres));
            }

            groupedClauses.Add("GROUP BY " + string.Join(", ", groupBys));
            if (havings.Count > 0)
            {
                groupedClauses.Add("HAVING " + string.Join(" AND ", havings));
            }

            string groupedInner = string.Join(nl + sp2, groupedClauses);
            return $"({nl}{sp}SELECT {selectExpr}{nl}{sp}FROM ({nl}{sp2}{groupedInner}{nl}{sp}){nl})";
        }

        if (stringEnumNameWrapType is { } enumNameWrapType)
        {
            clauses[0] = $"SELECT {distinctKeyword}{selectExpr} AS \"value\"";
            string enumInnerSelect = string.Join(nl + sp, clauses);
            SQLiteExpression nameText = EnumMemberVisitor.BuildEnumToNameInline(visitor, enumNameWrapType, "\"value\"");
            parameters.AddRange(nameText.Parameters!);
            return $"({nl}{sp}SELECT {nameText}{nl}{sp}FROM ({nl}{sp2}{enumInnerSelect}{nl}{sp}){nl})";
        }

        return $"({nl}{sp}{innerSelect}{nl})";
    }
}
