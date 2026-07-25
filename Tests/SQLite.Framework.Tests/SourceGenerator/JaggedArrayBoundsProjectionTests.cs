using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21kJaggedRows")]
public class H21kJaggedRow
{
    [Key]
    public int Id { get; set; }

    public int Size { get; set; }
}

public class JaggedArrayBoundsProjectionTests
{
    private static List<H21kJaggedRow> Rows()
    {
        return
        [
            new H21kJaggedRow { Id = 1, Size = 3 },
            new H21kJaggedRow { Id = 2, Size = 0 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21kJaggedRow>().Schema.CreateTable();
        db.Table<H21kJaggedRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ColumnBoundJaggedArrayProjectionMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new int[r.Size][])
            .Select(a => a.Length)
            .ToList();

        List<int> actual = db.Table<H21kJaggedRow>()
            .OrderBy(r => r.Id)
            .Select(r => new int[r.Size][])
            .ToList()
            .Select(a => a.Length)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstantBoundJaggedArrayProjectionMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new int[2][])
            .Select(a => a.Length)
            .ToList();

        List<int> actual = db.Table<H21kJaggedRow>()
            .OrderBy(r => r.Id)
            .Select(r => new int[2][])
            .ToList()
            .Select(a => a.Length)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
