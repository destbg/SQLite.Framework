using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24nCtorSourceRows")]
public class H24nCtorSourceRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H24nCtorTargetRows")]
public class H24nCtorTargetRow
{
    public H24nCtorTargetRow()
    {
    }

    public H24nCtorTargetRow(int id, string name)
    {
        Id = id;
        Name = name;
    }

    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H24nCtorSeededTargetRows")]
public class H24nCtorSeededTargetRow
{
    public H24nCtorSeededTargetRow()
    {
    }

    public H24nCtorSeededTargetRow(int seed)
    {
        Total = seed + 1;
    }

    [Key]
    public int Id { get; set; }

    public int Total { get; set; }
}

public class InsertFromQueryConstructorProjectionTests
{
    [Fact]
    public void ConstructorProjectionCopiesEveryColumn()
    {
        using TestDatabase db = Setup(nameof(ConstructorProjectionCopiesEveryColumn));

        db.Table<H24nCtorTargetRow>().InsertFromQuery(
            db.Table<H24nCtorSourceRow>().Select(s => new H24nCtorTargetRow(s.Id, s.Name)));

        List<(int Id, string Name)> expected = Rows()
            .Select(s => new H24nCtorTargetRow(s.Id, s.Name))
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, t.Name))
            .ToList();

        List<(int Id, string Name)> actual = db.Table<H24nCtorTargetRow>()
            .OrderBy(t => t.Id)
            .ToList()
            .Select(t => (t.Id, t.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstructorProjectionWithAnUnmatchedArgumentNameReportsIt()
    {
        using TestDatabase db = Setup(nameof(ConstructorProjectionWithAnUnmatchedArgumentNameReportsIt));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            db.Table<H24nCtorSeededTargetRow>().InsertFromQuery(
                db.Table<H24nCtorSourceRow>().Select(s => new H24nCtorSeededTargetRow(s.Id))));

        Assert.Contains("seed", exception.Message);
    }

    private static List<H24nCtorSourceRow> Rows()
    {
        return
        [
            new H24nCtorSourceRow { Id = 1, Name = "alpha" },
            new H24nCtorSourceRow { Id = 2, Name = "beta" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24nCtorSourceRow>().Schema.CreateTable();
        db.Table<H24nCtorTargetRow>().Schema.CreateTable();
        db.Table<H24nCtorSourceRow>().AddRange(Rows());
        return db;
    }
}
