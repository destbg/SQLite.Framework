namespace SQLite.Framework.Internals.JSON;

internal partial class JsonCollectionVisitor
{
    private string BuildSql(string sourceSql)
    {
        string sp = new(' ', (visitor.Level + 1) * 4);
        string sp2 = new(' ', (visitor.Level + 2) * 4);
        string nl = Environment.NewLine;

        string distinctKeyword = distinct ? "DISTINCT " : "";
        string tableAlias = crossJoin != null ? " e" : "";
        string joinClause = crossJoin ?? "";

        List<string> clauses = [$"SELECT {distinctKeyword}{selectExpr}", $"FROM json_each({sourceSql}){tableAlias}{joinClause}"];

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

        if (limit != null && offset != null)
        {
            clauses.Add($"LIMIT {limit} OFFSET {offset}");
        }
        else if (limit != null)
        {
            clauses.Add($"LIMIT {limit}");
        }
        else if (offset != null)
        {
            clauses.Add($"LIMIT -1 OFFSET {offset}");
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
            bool needsSubquery = orderBys.Count > 0 || limit != null || offset != null;
            if (needsSubquery)
            {
                string innerSelect2 = string.Join(nl + sp2, clauses);
                return $"({nl}{sp}SELECT json_group_array({(distinct ? "DISTINCT " : "")}value){nl}{sp}FROM ({nl}{sp2}{innerSelect2}{nl}{sp}){nl})";
            }

            clauses[0] = $"SELECT json_group_array({distinctKeyword}{selectExpr})";
            string simpleSelect = string.Join(nl + sp, clauses);
            return $"({nl}{sp}{simpleSelect}{nl})";
        }

        return $"({nl}{sp}{innerSelect}{nl})";
    }
}
