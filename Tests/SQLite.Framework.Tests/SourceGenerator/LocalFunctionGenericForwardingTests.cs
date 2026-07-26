using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22nLocalStepRows")]
public class H22nLocalStepRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H22nLocalStepViewBase
{
    public int Id { get; set; }

    public string Label { get; set; } = "";
}

public class H22nLocalStepView : H22nLocalStepViewBase
{
}

public class LocalFunctionGenericForwardingTests
{
    [Fact]
    public void ProjectionForwardedInsideALocalFunctionMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<(int Id, string Label)> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, Label: r.Name))
            .ToList();

        List<(int Id, string Label)> actual = ProjectVia<H22nLocalStepView>(db.Table<H22nLocalStepRow>())
            .OrderBy(v => v.Id)
            .Select(v => (v.Id, v.Label))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22nLocalStepRow> Rows()
    {
        return
        [
            new H22nLocalStepRow { Id = 1, Name = "a" },
            new H22nLocalStepRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22nLocalStepRow>().Schema.CreateTable();
        db.Table<H22nLocalStepRow>().AddRange(Rows());
        return db;
    }

    private static List<TView> ProjectVia<TView>(IQueryable<H22nLocalStepRow> query)
        where TView : H22nLocalStepViewBase, new()
    {
        List<TView> Run()
        {
            return ProjectCore<TView>(query);
        }

        return Run();
    }

    private static List<TView> ProjectCore<TView>(IQueryable<H22nLocalStepRow> query)
        where TView : H22nLocalStepViewBase, new()
    {
        return query.Select(r => new TView { Id = r.Id, Label = r.Name }).ToList();
    }
}
