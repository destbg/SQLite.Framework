using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23qCatalogRows")]
public class H23qCatalogRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    [NotMapped]
    public List<string> Tags { get; set; } = [];
}

[Table("H23qCountRows")]
public class H23qCountRow
{
    public int N { get; set; }

    public int Squared => N * N;
}

[Table("H23qPlainCountRows")]
public class H23qPlainCountRow
{
    public int N { get; set; }
}

[Table("H23qStampedRows")]
public class H23qStampedRow
{
    public static string Stamp { get; set; } = "s";

    public int N { get; set; }

    public string? Label { get; set; }

    public string Hidden
    {
        set => Label = value;
    }
}

public class ValueSourceUnmappedMemberTests
{
    [Fact]
    public void ValuesRangeConcatWithAPlainTableReadsEveryRow()
    {
        using TestDatabase db = new();
        db.Table<H23qPlainCountRow>().Schema.CreateTable();
        db.Table<H23qPlainCountRow>().AddRange(StoredPlainCounts());

        List<int> expected = SeedPlainCounts()
            .Concat(StoredPlainCounts())
            .Select(r => r.N)
            .OrderBy(n => n)
            .ToList();

        List<int> actual = db.ValuesRange(SeedPlainCounts())
            .Concat(db.Table<H23qPlainCountRow>())
            .ToList()
            .Select(r => r.N)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ValuesRangeOverAnEntityWithAnUnmappedCollectionMemberReadsTheMappedColumns()
    {
        using TestDatabase db = new();
        List<H23qCatalogRow> rows = CatalogRows();

        List<string> expected = rows.Select(r => r.Name).ToList();
        List<string> actual = db.ValuesRange(rows).Select(r => r.Name).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ValuesRangeConcatWithItsTableReadsEveryRow()
    {
        using TestDatabase db = new();
        db.Table<H23qCountRow>().Schema.CreateTable();
        db.Table<H23qCountRow>().AddRange(StoredCounts());

        List<int> expected = SeedCounts()
            .Concat(StoredCounts())
            .Select(r => r.N)
            .OrderBy(n => n)
            .ToList();

        List<int> actual = db.ValuesRange(SeedCounts())
            .Concat(db.Table<H23qCountRow>())
            .ToList()
            .Select(r => r.N)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ValuesRangeWithANullRowReadsNullCells()
    {
        using TestDatabase db = new();

        List<int?> actual = db.ValuesRange([new H23qStampedRow { N = 4, Label = "x" }, null!])
            .Select(r => (int?)r.N)
            .ToList();

        Assert.Equal([4, null], actual);
    }

    [Fact]
    public void RecursiveCommonTableExpressionSeededFromAValuesRowCountsUp()
    {
        using TestDatabase db = new();

        SQLiteCte<H23qCountRow> cte = db.WithRecursive<H23qCountRow>(self =>
            db.Values(new H23qCountRow { N = 1 })
                .Concat(from p in self where p.N < 3 select new H23qCountRow { N = p.N + 1 }));

        List<int> actual = (from p in cte select p)
            .ToList()
            .Select(p => p.N)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal([1, 2, 3], actual);
    }

    private static List<H23qCatalogRow> CatalogRows()
    {
        return
        [
            new H23qCatalogRow { Id = 1, Name = "Ann", Tags = ["red"] },
            new H23qCatalogRow { Id = 2, Name = "Bob", Tags = ["blue", "green"] }
        ];
    }

    private static List<H23qCountRow> SeedCounts()
    {
        return
        [
            new H23qCountRow { N = 1 },
            new H23qCountRow { N = 2 }
        ];
    }

    private static List<H23qCountRow> StoredCounts()
    {
        return
        [
            new H23qCountRow { N = 3 },
            new H23qCountRow { N = 4 }
        ];
    }

    private static List<H23qPlainCountRow> SeedPlainCounts()
    {
        return
        [
            new H23qPlainCountRow { N = 1 },
            new H23qPlainCountRow { N = 2 }
        ];
    }

    private static List<H23qPlainCountRow> StoredPlainCounts()
    {
        return
        [
            new H23qPlainCountRow { N = 3 },
            new H23qPlainCountRow { N = 4 }
        ];
    }
}
