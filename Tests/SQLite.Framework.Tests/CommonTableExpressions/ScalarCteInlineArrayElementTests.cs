using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21nIdxRows")]
public class H21nIdxRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class ScalarCteInlineArrayElementTests
{
    private static List<H21nIdxRow> Rows()
    {
        return
        [
            new H21nIdxRow { Id = 1, A = 10, B = 100 },
            new H21nIdxRow { Id = 2, A = 20, B = 200 },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21nIdxRow>().Schema.CreateTable();
        db.Table<H21nIdxRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void TableInlineArrayElementProjectionMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new[] { r.A, r.B }[0])
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H21nIdxRow>()
            .Select(r => new[] { r.A, r.B }[0])
            .ToList()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ScalarCteInlineArrayElementBodyMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new[] { r.A, r.B }[0])
            .Select(v => v * 2)
            .OrderBy(v => v)
            .ToList();

        SQLiteCte<int> cte = db.With(() => db.Table<H21nIdxRow>()
            .Select(r => new[] { r.A, r.B }[0]));

        List<int> actual = cte
            .Select(v => v * 2)
            .ToList()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
