using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22nExtensionRows")]
public class H22nExtensionRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H22nExtensionViewBase
{
    public int Id { get; set; }

    public string Label { get; set; } = "";
}

public class H22nExtensionView : H22nExtensionViewBase
{
}

public static class H22nExtensionProjections
{
    public static List<TView> ProjectRows<TView>(this IQueryable<H22nExtensionRow> query)
        where TView : H22nExtensionViewBase, new()
    {
        return query.Select(r => new TView { Id = r.Id, Label = r.Name }).ToList();
    }
}

public class GenericExtensionMethodProjectionTests
{
    [Fact]
    public void ProjectionThroughAGenericExtensionMethodMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<(int Id, string Label)> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, Label: r.Name))
            .ToList();

        List<(int Id, string Label)> actual = db.Table<H22nExtensionRow>()
            .ProjectRows<H22nExtensionView>()
            .OrderBy(v => v.Id)
            .Select(v => (v.Id, v.Label))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22nExtensionRow> Rows()
    {
        return
        [
            new H22nExtensionRow { Id = 1, Name = "a" },
            new H22nExtensionRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22nExtensionRow>().Schema.CreateTable();
        db.Table<H22nExtensionRow>().AddRange(Rows());
        return db;
    }
}
