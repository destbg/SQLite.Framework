using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24bScaleRows")]
public class H24bScaleRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H24bScaled
{
    public H24bScaled(int value)
    {
        Value = value * 10;
    }

    public int Value { get; set; }
}

public class H24bDropped
{
    public H24bDropped(int raw)
    {
        Doubled = raw * 2;
    }

    public int Raw { get; set; }

    public int Doubled { get; set; }
}

public class H24bScaleOuter
{
    public int Id { get; set; }

    public H24bScaled? Scaled { get; set; }
}

public class H24bDroppedOuter
{
    public int Id { get; set; }

    public H24bDropped? Dropped { get; set; }
}

public class ConstructorTransformedMemberReadTests
{
    [Fact]
    public void NestedConstructorThatTransformsItsArgumentReadsTheStoredValue()
    {
        using TestDatabase db = Setup(nameof(NestedConstructorThatTransformsItsArgumentReadsTheStoredValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H24bScaleOuter { Id = r.Id, Scaled = new H24bScaled(r.A) })
            .Select(o => o.Scaled!.Value)
            .ToList();

        List<int> actual = db.Table<H24bScaleRow>().OrderBy(r => r.Id)
            .Select(r => new H24bScaleOuter { Id = r.Id, Scaled = new H24bScaled(r.A) })
            .Select(o => o.Scaled!.Value)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedConstructorThatNeverStoresItsArgumentReadsTheDefault()
    {
        using TestDatabase db = Setup(nameof(NestedConstructorThatNeverStoresItsArgumentReadsTheDefault));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H24bDroppedOuter { Id = r.Id, Dropped = new H24bDropped(r.A) })
            .Select(o => o.Dropped!.Raw)
            .ToList();

        List<int> actual = db.Table<H24bScaleRow>().OrderBy(r => r.Id)
            .Select(r => new H24bDroppedOuter { Id = r.Id, Dropped = new H24bDropped(r.A) })
            .Select(o => o.Dropped!.Raw)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnonymousOuterWithATransformingConstructorReadsTheStoredValue()
    {
        using TestDatabase db = Setup(nameof(AnonymousOuterWithATransformingConstructorReadsTheStoredValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Scaled = new H24bScaled(r.A) })
            .Select(o => o.Scaled.Value)
            .ToList();

        List<int> actual = db.Table<H24bScaleRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Scaled = new H24bScaled(r.A) })
            .Select(o => o.Scaled.Value)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TopLevelConstructorThatTransformsItsArgumentReadsTheStoredValue()
    {
        using TestDatabase db = Setup(nameof(TopLevelConstructorThatTransformsItsArgumentReadsTheStoredValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H24bScaled(r.A))
            .Select(s => s.Value)
            .ToList();

        List<int> actual = db.Table<H24bScaleRow>().OrderBy(r => r.Id)
            .Select(r => new H24bScaled(r.A))
            .Select(s => s.Value)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24bScaleRow> Rows()
    {
        return
        [
            new H24bScaleRow { Id = 1, A = 3 },
            new H24bScaleRow { Id = 2, A = 7 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(methodName);
        db.Table<H24bScaleRow>().Schema.CreateTable();
        db.Table<H24bScaleRow>().AddRange(Rows());
        return db;
    }
}
