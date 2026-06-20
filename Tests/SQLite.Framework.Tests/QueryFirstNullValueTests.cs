using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class QueryFirstNullValueTests
{
    [Fact]
    public void QueryFirstReturnsFirstRowWhenItsValueIsNull()
    {
        using TestDatabase db = new();

        List<string> rows = db.Query<string>("SELECT NULL", []);
        string expected = rows.First();

        string actual = db.QueryFirst<string>("SELECT NULL", []);

        Assert.Equal(expected, actual);
    }
}
