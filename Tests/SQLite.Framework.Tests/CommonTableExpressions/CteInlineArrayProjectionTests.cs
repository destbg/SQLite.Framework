using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20ArrCte")]
public class H20ArrCteRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class CteInlineArrayProjectionTests
{
    [Fact]
    public void CteBodyArrayMemberPlainColumnReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { r.Id, Arr = new[] { r.A, r.B } })
            .Select(x => x.Id).ToList();

        List<int> actual = db.With(() => db.Table<H20ArrCteRow>()
                .Select(r => new { r.Id, Arr = new[] { r.A, r.B } }))
            .Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteBodyArrayMemberArrayReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int[]> expected = Rows()
            .Select(r => new { r.Id, Arr = new[] { r.A, r.B } })
            .Select(x => x.Arr)
            .ToList();

        List<int[]> actual = db.With(() => db.Table<H20ArrCteRow>()
                .Select(r => new { r.Id, Arr = new[] { r.A, r.B } }))
            .Select(x => x.Arr)
            .ToList();

        AssertArraysEqual(expected, actual);
    }

    [Fact]
    public void DirectProjectionArrayMemberArrayReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int[]> expected = Rows()
            .Select(r => new { r.Id, Arr = new[] { r.A, r.B } })
            .Select(x => x.Arr)
            .ToList();

        List<int[]> actual = db.Table<H20ArrCteRow>()
            .Select(r => new { r.Id, Arr = new[] { r.A, r.B } })
            .Select(x => x.Arr)
            .ToList();

        AssertArraysEqual(expected, actual);
    }

    [Fact]
    public void CteBodyTopLevelArrayMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int[]> expected = Rows()
            .Select(r => new[] { r.A, r.B })
            .ToList();

        List<int[]> actual = db.With(() => db.Table<H20ArrCteRow>()
                .Select(r => new[] { r.A, r.B }))
            .ToList();

        AssertArraysEqual(expected, actual);
    }

    [Fact]
    public void DirectProjectionArrayMemberReadOverEmptySourceMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int[]> expected = Rows()
            .Where(r => r.Id > 100)
            .Select(r => new { r.Id, Arr = new[] { r.A, r.B } })
            .Select(x => x.Arr)
            .ToList();

        List<int[]> actual = db.Table<H20ArrCteRow>()
            .Where(r => r.Id > 100)
            .Select(r => new { r.Id, Arr = new[] { r.A, r.B } })
            .Select(x => x.Arr)
            .ToList();

        AssertArraysEqual(expected, actual);
    }

    [Fact]
    public void CteBodyBoundsArrayMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { r.Id, Arr = new int[r.A] })
            .Select(x => x.Id).ToList();

        List<int> actual = db.With(() => db.Table<H20ArrCteRow>()
                .Select(r => new { r.Id, Arr = new int[r.A] }))
            .Select(x => x.Id).ToList();

        Assert.Equal(expected, actual);
    }

    private static void AssertArraysEqual(List<int[]> expected, List<int[]> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    private static List<H20ArrCteRow> Rows()
    {
        return
        [
            new H20ArrCteRow { Id = 1, A = 10, B = 100 },
            new H20ArrCteRow { Id = 2, A = 20, B = 200 },
            new H20ArrCteRow { Id = 3, A = 0, B = -5 },
            new H20ArrCteRow { Id = 4, A = 10, B = 100 },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H20ArrCteRow>().Schema.CreateTable();
        db.Table<H20ArrCteRow>().AddRange(Rows());
        return db;
    }
}
