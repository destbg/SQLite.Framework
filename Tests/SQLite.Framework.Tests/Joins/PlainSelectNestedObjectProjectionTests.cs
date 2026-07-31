using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PlainSelectNestedObjectProjectionTests
{
    [Fact]
    public void ASingleParameterSelectWrapsAProjectedNestedObject()
    {
        using TestDatabase db = Setup(nameof(ASingleParameterSelectWrapsAProjectedNestedObject));

        List<int> expected = Rows()
            .Select(r => new H26aSideOuter { K = r.K, Part = new H26aSidePart { P = r.A } })
            .Select(x => new { x.K, Wrapped = x.Part })
            .Select(x => x.Wrapped!.P)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H26aSideRow>()
            .Select(r => new H26aSideOuter { K = r.K, Part = new H26aSidePart { P = r.A } })
            .Select(x => new { x.K, Wrapped = x.Part })
            .AsEnumerable()
            .Select(x => x.Wrapped!.P)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ASingleParameterSelectReadsABareProjectedNestedObject()
    {
        using TestDatabase db = Setup(nameof(ASingleParameterSelectReadsABareProjectedNestedObject));

        List<int> expected = Rows()
            .Select(r => new H26aSideOuter { K = r.K, Part = new H26aSidePart { P = r.A } })
            .Select(o => o.Part!)
            .Select(part => part.P)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H26aSideRow>()
            .Select(r => new H26aSideOuter { K = r.K, Part = new H26aSidePart { P = r.A } })
            .Select(o => o.Part!)
            .AsEnumerable()
            .Select(part => part.P)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ASingleParameterSelectWrapsAnOptionalJoinedEntity()
    {
        using TestDatabase db = Setup(nameof(ASingleParameterSelectWrapsAnOptionalJoinedEntity));

        List<int> expected = Rows()
            .GroupJoin(Rows(), a => a.K, b => b.K, (a, bs) => new { a, bs })
            .SelectMany(t => t.bs.DefaultIfEmpty(), (t, b) => new { t.a.Id, B = b })
            .Select(x => new { x.Id, Wrap = x.B })
            .Select(x => x.Wrap!.A)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H26aSideRow>()
            .GroupJoin(db.Table<H26aSideRow>(), a => a.K, b => b.K, (a, bs) => new { a, bs })
            .SelectMany(t => t.bs.DefaultIfEmpty(), (t, b) => new { t.a.Id, B = b })
            .Select(x => new { x.Id, Wrap = x.B })
            .AsEnumerable()
            .Select(x => x.Wrap!.A)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26aSideRow> Rows()
    {
        return
        [
            new H26aSideRow { Id = 1, K = 1, A = 5, B = 50 },
            new H26aSideRow { Id = 2, K = 2, A = 6, B = 60 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26aSideRow>().Schema.CreateTable();
        db.Table<H26aSideRow>().AddRange(Rows());
        return db;
    }
}
