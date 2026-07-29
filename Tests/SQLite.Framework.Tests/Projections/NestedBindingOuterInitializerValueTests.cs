using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24bBindRows")]
public class H24bBindRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H24bBindNested
{
    public int X { get; set; }

    public int Tag { get; set; }
}

public class H24bBindOuter
{
    public int Id { get; set; }

    public H24bBindNested Nested { get; set; } = new H24bBindNested { Tag = 7 };
}

public class NestedBindingOuterInitializerValueTests
{
    [Fact]
    public void MemberBoundNestedObjectKeepsTheValueTheOuterInitializerGaveIt()
    {
        using TestDatabase db = Setup(nameof(MemberBoundNestedObjectKeepsTheValueTheOuterInitializerGaveIt));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H24bBindOuter { Id = r.Id, Nested = { X = r.A } })
            .Select(o => o.Nested.Tag)
            .ToList();

        List<int> actual = db.Table<H24bBindRow>().OrderBy(r => r.Id)
            .Select(r => new H24bBindOuter { Id = r.Id, Nested = { X = r.A } })
            .Select(o => o.Nested.Tag)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MemberBoundNestedObjectReadsTheColumnItWasGiven()
    {
        using TestDatabase db = Setup(nameof(MemberBoundNestedObjectReadsTheColumnItWasGiven));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H24bBindOuter { Id = r.Id, Nested = { X = r.A } })
            .Select(o => o.Nested.X)
            .ToList();

        List<int> actual = db.Table<H24bBindRow>().OrderBy(r => r.Id)
            .Select(r => new H24bBindOuter { Id = r.Id, Nested = { X = r.A } })
            .Select(o => o.Nested.X)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24bBindRow> Rows()
    {
        return
        [
            new H24bBindRow { Id = 1, A = 11 },
            new H24bBindRow { Id = 2, A = 22 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(methodName);
        db.Table<H24bBindRow>().Schema.CreateTable();
        db.Table<H24bBindRow>().AddRange(Rows());
        return db;
    }
}
