namespace SQLite.Framework.Internals.JSON;

internal partial class JsonCollectionVisitor
{
    private string BuildSql(string sourceSql)
    {
        string sp = new(' ', (visitor.Level + 1) * 4);
        string sp2 = new(' ', (visitor.Level + 2) * 4);
        string nl = Environment.NewLine;

        string distinctKeyword = distinct ? "DISTINCT " : "";
        string joinClause = crossJoin ?? "";
        string fromClause = fromOverride ?? $"json_each({sourceSql}){baseJoinSuffix}{joinClause}";

        List<string> clauses = [$"SELECT {distinctKeyword}{selectExpr}", $"FROM {fromClause}"];

        if (wheres.Count > 0)
        {
            clauses.Add("WHERE " + string.Join(" AND ", wheres));
        }

        if (groupBys.Count > 0)
        {
            clauses.Add("GROUP BY " + string.Join(", ", groupBys));
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
            List<string> countClauses = [.. clauses];
            countClauses[0] = "SELECT COUNT(*)";
            countClauses.Add("LIMIT 2");
            string countSelect = string.Join(nl + sp2, countClauses);

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
                return $"({nl}{sp}SELECT json_group_array(\"value\"){nl}{sp}FROM ({nl}{sp2}{groupedInner}{nl}{sp}){nl})";
            }

            if (distinct && reverseApplied)
            {
                string positionAggregate = distinctSeenReverse ? "MAX" : "MIN";
                List<string> comboClauses = [$"SELECT {selectExpr} AS \"value\"", $"FROM {fromClause}"];
                if (wheres.Count > 0)
                {
                    comboClauses.Add("WHERE " + string.Join(" AND ", wheres));
                }

                comboClauses.Add($"GROUP BY {selectExpr}");
                comboClauses.Add($"ORDER BY {positionAggregate}({keyColumn}) DESC");
                string comboInner = string.Join(nl + sp2, comboClauses);
                return $"({nl}{sp}SELECT json_group_array(\"value\"){nl}{sp}FROM ({nl}{sp2}{comboInner}{nl}{sp}){nl})";
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
                return $"({nl}{sp}SELECT json_group_array({(distinct ? "DISTINCT " : "")}{arrayColumn}){nl}{sp}FROM ({nl}{sp2}{innerSelect2}{nl}{sp}){nl})";
            }

            clauses[0] = $"SELECT json_group_array({distinctKeyword}{selectExpr})";
            string simpleSelect = string.Join(nl + sp, clauses);
            return $"({nl}{sp}{simpleSelect}{nl})";
        }

        return $"({nl}{sp}{innerSelect}{nl})";
    }
}
