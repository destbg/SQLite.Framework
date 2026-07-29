using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24mArrayLengthRows")]
public class H24mArrayLengthRow
{
    [Key]
    public int Id { get; set; }

    public byte[] Data { get; set; } = [];
}

public static class H24mArrayLengthFunctions
{
    public static string Describe(int size)
    {
        return "n=" + size;
    }
}

public class ArrayLengthProjectionTests
{
    [Fact]
    public void CapturedArrayLengthInsideAClientCallMatchesLinq()
    {
        using TestDatabase db = Setup();
        int[] sizes = [1, 2, 3];

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H24mArrayLengthFunctions.Describe(sizes.Length + r.Id))
            .ToList();

        List<string> actual = db.Table<H24mArrayLengthRow>()
            .OrderBy(r => r.Id)
            .Select(r => H24mArrayLengthFunctions.Describe(sizes.Length + r.Id))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BlobColumnLengthInsideAClientCallMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H24mArrayLengthFunctions.Describe(r.Data.Length))
            .ToList();

        List<string> actual = db.Table<H24mArrayLengthRow>()
            .OrderBy(r => r.Id)
            .Select(r => H24mArrayLengthFunctions.Describe(r.Data.Length))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24mArrayLengthRow> Rows()
    {
        return
        [
            new H24mArrayLengthRow { Id = 1, Data = [1, 2] },
            new H24mArrayLengthRow { Id = 2, Data = [3, 4, 5, 6] }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H24mArrayLengthRow>().Schema.CreateTable();
        db.Table<H24mArrayLengthRow>().AddRange(Rows());
        return db;
    }
}
