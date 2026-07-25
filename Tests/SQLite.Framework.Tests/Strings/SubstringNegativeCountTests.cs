using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SubstringNegativeCountTests
{
    [Fact]
    public void SubstringWithNegativeCountFollowsSqliteSubstr()
    {
        using TestDatabase db = new();
        db.Table<TwoStringEntity>().Schema.CreateTable();
        db.Table<TwoStringEntity>().Add(new TwoStringEntity { Id = 1, A = "hello", B = "" });

        Assert.Throws<ArgumentOutOfRangeException>(() => "hello".Substring(2, -1));

        string framework = db.Table<TwoStringEntity>()
            .Select(x => x.A.Substring(2, -1))
            .First();
        string sqliteSubstr = db.ExecuteScalar<string>("SELECT SUBSTR('hello', 3, -1)")!;

        Assert.Equal(sqliteSubstr, framework);
    }
}
