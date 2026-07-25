using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ArrLitRow")]
public class ArrLitRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class ProjectedMemberArrayLiteralTests
{
    private static List<ArrLitRow> Rows() =>
    [
        new ArrLitRow { Id = 1, A = 10, B = 100 },
        new ArrLitRow { Id = 2, A = 20, B = 200 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<ArrLitRow>().Schema.CreateTable();
        db.Table<ArrLitRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void FlatColumnArrayLiteralMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int[]> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new[] { r.A, r.B }).ToList();

        var actual = db.Table<ArrLitRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Arr = new[] { r.A, r.B } })
            .ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], actual[i].Arr);
        }
    }

    [Fact]
    public void NestedArrayLiteralMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int[][]> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new[] { new[] { r.A }, new[] { r.B } }).ToList();

        var actual = db.Table<ArrLitRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Arr = new[] { new[] { r.A }, new[] { r.B } } })
            .ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], actual[i].Arr);
        }
    }

    [Fact]
    public void BoundsFormArrayMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int[]> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new int[2]).ToList();

        var actual = db.Table<ArrLitRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Arr = new int[2] })
            .ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], actual[i].Arr);
        }
    }
}
