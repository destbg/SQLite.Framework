using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23mStoredProjectionRows")]
public class H23mStoredProjectionRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H23mStoredProjectionFunctions
{
    public static string Decorate(string name)
    {
        return "<" + name + ">";
    }
}

public class StoredProjectionExpressionTests
{
    [Fact]
    public void ProjectionHeldInAnExpressionVariableMatchesLinq()
    {
        using TestDatabase db = Setup();

        Expression<Func<H23mStoredProjectionRow, string>> projection =
            r => H23mStoredProjectionFunctions.Decorate(r.Name);

        List<string> expected = Rows()
            .ConvertAll(r => H23mStoredProjectionFunctions.Decorate(r.Name));

        List<string> actual = db.Table<H23mStoredProjectionRow>()
            .OrderBy(r => r.Id)
            .Select(projection)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23mStoredProjectionRow> Rows()
    {
        return
        [
            new H23mStoredProjectionRow { Id = 1, Name = "a" },
            new H23mStoredProjectionRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H23mStoredProjectionRow>().Schema.CreateTable();
        db.Table<H23mStoredProjectionRow>().AddRange(Rows());
        return db;
    }
}
