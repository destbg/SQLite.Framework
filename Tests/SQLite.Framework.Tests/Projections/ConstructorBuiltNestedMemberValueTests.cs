using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23bCtorNestRows")]
public class H23bCtorNestRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H23bCtorSide
{
    public H23bCtorSide(int x)
    {
        X = x;
        Doubled = x * 2;
    }

    public int X { get; set; }

    public int Doubled { get; set; }

    public int Preset { get; set; } = 9;
}

public class H23bDualCtorSide
{
    public H23bDualCtorSide()
    {
    }

    public H23bDualCtorSide(int x)
    {
        X = x;
        Doubled = x * 2;
    }

    public int X { get; set; }

    public int Doubled { get; set; }
}

public class H23bCtorNestOuter
{
    public int Id { get; set; }

    public H23bCtorSide? Side { get; set; }
}

public class H23bDualCtorNestOuter
{
    public int Id { get; set; }

    public H23bDualCtorSide? Side { get; set; }
}

public class ConstructorBuiltNestedMemberValueTests
{
    [Fact]
    public void ConstructorAssignedMemberOfANamedNestedObjectReadsItsValue()
    {
        using TestDatabase db = Setup(nameof(ConstructorAssignedMemberOfANamedNestedObjectReadsItsValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H23bCtorNestOuter { Id = r.Id, Side = new H23bCtorSide(r.A) })
            .Select(o => o.Side!.Doubled)
            .ToList();

        List<int> actual = db.Table<H23bCtorNestRow>().OrderBy(r => r.Id)
            .Select(r => new H23bCtorNestOuter { Id = r.Id, Side = new H23bCtorSide(r.A) })
            .Select(o => o.Side!.Doubled)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PropertyInitializerOfANamedNestedObjectReadsItsInitialValue()
    {
        using TestDatabase db = Setup(nameof(PropertyInitializerOfANamedNestedObjectReadsItsInitialValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H23bCtorNestOuter { Id = r.Id, Side = new H23bCtorSide(r.A) })
            .Select(o => o.Side!.Preset)
            .ToList();

        List<int> actual = db.Table<H23bCtorNestRow>().OrderBy(r => r.Id)
            .Select(r => new H23bCtorNestOuter { Id = r.Id, Side = new H23bCtorSide(r.A) })
            .Select(o => o.Side!.Preset)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ChosenConstructorDecidesTheValueWhenTheTypeAlsoHasAParameterlessConstructor()
    {
        using TestDatabase db = Setup(nameof(ChosenConstructorDecidesTheValueWhenTheTypeAlsoHasAParameterlessConstructor));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H23bDualCtorNestOuter { Id = r.Id, Side = new H23bDualCtorSide(r.A) })
            .Select(o => o.Side!.Doubled)
            .ToList();

        List<int> actual = db.Table<H23bCtorNestRow>().OrderBy(r => r.Id)
            .Select(r => new H23bDualCtorNestOuter { Id = r.Id, Side = new H23bDualCtorSide(r.A) })
            .Select(o => o.Side!.Doubled)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilterOnAConstructorAssignedNestedMemberReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(FilterOnAConstructorAssignedNestedMemberReportsItCannotRun));

        Assert.Throws<NotSupportedException>(() => db.Table<H23bCtorNestRow>()
            .Select(r => new H23bCtorNestOuter { Id = r.Id, Side = new H23bCtorSide(r.A) })
            .Where(o => o.Side!.Doubled > 20)
            .Select(o => o.Id)
            .ToList());
    }

    [Fact]
    public void AnonymousOuterReadsTheSameConstructorAssignedMemberValue()
    {
        using TestDatabase db = Setup(nameof(AnonymousOuterReadsTheSameConstructorAssignedMemberValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Side = new H23bCtorSide(r.A) })
            .Select(o => o.Side.Doubled)
            .ToList();

        List<int> actual = db.Table<H23bCtorNestRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Side = new H23bCtorSide(r.A) })
            .Select(o => o.Side.Doubled)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstructorArgumentOfADoublyNestedObjectReadsItsValue()
    {
        using TestDatabase db = Setup(nameof(ConstructorArgumentOfADoublyNestedObjectReadsItsValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { D = new H23bCtorNestOuter { Id = r.Id, Side = new H23bCtorSide(r.A) } })
            .Select(x => x.D.Side!.X)
            .ToList();

        List<int> actual = db.Table<H23bCtorNestRow>().OrderBy(r => r.Id)
            .Select(r => new { D = new H23bCtorNestOuter { Id = r.Id, Side = new H23bCtorSide(r.A) } })
            .Select(x => x.D.Side!.X)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PlainRowMemberBesideAConstructedMemberReadsItsValue()
    {
        using TestDatabase db = Setup(nameof(PlainRowMemberBesideAConstructedMemberReadsItsValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { D = new H23bCtorSide(r.A), E = r })
            .Select(x => x.E.Id)
            .ToList();

        List<int> actual = db.Table<H23bCtorNestRow>().OrderBy(r => r.Id)
            .Select(r => new { D = new H23bCtorSide(r.A), E = r })
            .Select(x => x.E.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23bCtorNestRow> Rows()
    {
        return
        [
            new H23bCtorNestRow { Id = 1, A = 5 },
            new H23bCtorNestRow { Id = 2, A = 20 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23bCtorNestRow>().Schema.CreateTable();
        db.Table<H23bCtorNestRow>().AddRange(Rows());
        return db;
    }
}
