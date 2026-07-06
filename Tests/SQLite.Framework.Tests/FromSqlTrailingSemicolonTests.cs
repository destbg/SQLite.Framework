using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SemicolonSqlRows")]
public class SemicolonSqlRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class FromSqlTrailingSemicolonTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<SemicolonSqlRow>().Schema.CreateTable();
        db.Table<SemicolonSqlRow>().Add(new SemicolonSqlRow { Id = 1, Name = "a" });
        db.Table<SemicolonSqlRow>().Add(new SemicolonSqlRow { Id = 2, Name = "b" });
        return db;
    }

    [Fact]
    public void TrailingSemicolonStillComposes()
    {
        using TestDatabase db = Seed();

        List<int> ids = db.FromSql<SemicolonSqlRow>("SELECT * FROM SemicolonSqlRows;")
            .Select(r => r.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal([1, 2], ids);
    }

    [Fact]
    public void TrailingSemicolonAndWhitespaceStillComposesWithWhere()
    {
        using TestDatabase db = Seed();

        List<string> names = db.FromSql<SemicolonSqlRow>("SELECT * FROM SemicolonSqlRows ; \n")
            .Where(r => r.Id > 1)
            .Select(r => r.Name)
            .ToList();

        Assert.Equal(["b"], names);
    }
}
