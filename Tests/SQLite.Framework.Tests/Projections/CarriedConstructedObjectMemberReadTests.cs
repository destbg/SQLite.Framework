using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24bCarryRows")]
public class H24bCarryRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H24bCarrySide
{
    public H24bCarrySide(int seed)
    {
        Total = seed + 1;
    }

    public int Total { get; set; }

    public int Preset { get; set; } = 9;
}

public class H24bCarryOuter
{
    public int Id { get; set; }

    public H24bCarrySide? Side { get; set; }
}

public class CarriedConstructedObjectMemberReadTests
{
    [Fact]
    public void InitializedMemberOfANestedConstructedObjectReadsItsInitialValue()
    {
        using TestDatabase db = Setup(nameof(InitializedMemberOfANestedConstructedObjectReadsItsInitialValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H24bCarryOuter { Id = r.Id, Side = new H24bCarrySide(r.A) })
            .Select(o => o.Side!.Preset)
            .ToList();

        List<int> actual = db.Table<H24bCarryRow>().OrderBy(r => r.Id)
            .Select(r => new H24bCarryOuter { Id = r.Id, Side = new H24bCarrySide(r.A) })
            .Select(o => o.Side!.Preset)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InitializedMemberStillReadsItsInitialValueAfterTheObjectIsWrapped()
    {
        using TestDatabase db = Setup(nameof(InitializedMemberStillReadsItsInitialValueAfterTheObjectIsWrapped));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H24bCarryOuter { Id = r.Id, Side = new H24bCarrySide(r.A) })
            .Select(o => new { o.Id, W = o })
            .Select(x => x.W.Side!.Preset)
            .ToList();

        List<int> actual = db.Table<H24bCarryRow>().OrderBy(r => r.Id)
            .Select(r => new H24bCarryOuter { Id = r.Id, Side = new H24bCarrySide(r.A) })
            .Select(o => new { o.Id, W = o })
            .Select(x => x.W.Side!.Preset)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilteringOnAnInitializedMemberOfAWrappedConstructedObjectReportsItCannotRun()
    {
        using TestDatabase db = Setup(nameof(FilteringOnAnInitializedMemberOfAWrappedConstructedObjectReportsItCannotRun));

        Assert.Throws<NotSupportedException>(() => db.Table<H24bCarryRow>()
            .Select(r => new H24bCarryOuter { Id = r.Id, Side = new H24bCarrySide(r.A) })
            .Select(o => new { o.Id, W = o })
            .Where(x => x.W.Side!.Preset == 9)
            .Select(x => x.Id)
            .ToList());
    }

    private static List<H24bCarryRow> Rows()
    {
        return
        [
            new H24bCarryRow { Id = 1, A = 4 },
            new H24bCarryRow { Id = 2, A = 8 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(methodName);
        db.Table<H24bCarryRow>().Schema.CreateTable();
        db.Table<H24bCarryRow>().AddRange(Rows());
        return db;
    }
}
