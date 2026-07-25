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
    private static List<H20ArrCteRow> Rows() =>
    [
        new H20ArrCteRow { Id = 1, A = 10, B = 100 },
        new H20ArrCteRow { Id = 2, A = 20, B = 200 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H20ArrCteRow>().Schema.CreateTable();
        db.Table<H20ArrCteRow>().AddRange(Rows());
        return db;
    }

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
    public void CteBodyArrayMemberArrayReadThrowsClean()
    {
        using TestDatabase db = Setup();

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.With(() => db.Table<H20ArrCteRow>()
                .Select(r => new { r.Id, Arr = new[] { r.A, r.B } }))
            .Select(x => x.Arr).ToList());

        Assert.Equal("Cannot read a query result into the collection type 'System.Int32[]'.", exception.Message);
    }

    [Fact]
    public void DirectProjectionArrayMemberArrayReadThrowsClean()
    {
        using TestDatabase db = Setup();

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<H20ArrCteRow>()
            .Select(r => new { r.Id, Arr = new[] { r.A, r.B } })
            .Select(x => x.Arr).ToList());

        Assert.Equal("Cannot read a query result into the collection type 'System.Int32[]'.", exception.Message);
    }

    [Fact]
    public void CteBodyTopLevelArrayThrowsClean()
    {
        using TestDatabase db = Setup();

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.With(() => db.Table<H20ArrCteRow>()
                .Select(r => new[] { r.A, r.B }))
            .ToList());

        Assert.Equal("Cannot read a query result into the collection type 'System.Int32[]'.", exception.Message);
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
}
