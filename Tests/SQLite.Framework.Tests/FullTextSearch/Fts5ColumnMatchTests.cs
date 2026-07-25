using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class Fts5ColumnMatchTests
{
    [Fact]
    public void ColumnScopedMatchUsesMappedColumnName()
    {
        using TestDatabase db = new();
        db.Table<ColumnAlias_Search>().Schema.CreateTable();
        db.Table<ColumnAlias_Search>().Add(new ColumnAlias_Search { Id = 1, Body = "hello world" });

        List<ColumnAlias_Search> rows = db.Table<ColumnAlias_Search>()
            .Where(d => SQLiteFTS5Functions.Match(d.Body, "hello"))
            .ToList();

        Assert.Single(rows);
    }
}
