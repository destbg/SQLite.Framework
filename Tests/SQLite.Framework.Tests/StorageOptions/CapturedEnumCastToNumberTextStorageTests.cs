using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H24lServiceLevel
{
    Basic = 1,
    Plus = 2,
    Premium = 3
}

[Table("H24lEnumCastRows")]
public class H24lEnumCastRow
{
    [Key]
    public int Id { get; set; }

    public int Rank { get; set; }

    public long Score { get; set; }
}

public class CapturedEnumCastToNumberTextStorageTests
{
    [Fact]
    public void FiltersAnIntColumnAgainstACapturedEnumCastToInt()
    {
        H24lServiceLevel level = H24lServiceLevel.Premium;
        using TestDatabase db = Seed();
        List<H24lEnumCastRow> local = Rows();

        List<int> expected = local
            .Where(r => r.Rank == (int)level)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24lEnumCastRow>()
            .Where(r => r.Rank == (int)level)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FiltersALongColumnAgainstACapturedEnumCastToLong()
    {
        H24lServiceLevel level = H24lServiceLevel.Plus;
        using TestDatabase db = Seed();
        List<H24lEnumCastRow> local = Rows();

        List<int> expected = local
            .Where(r => r.Score == (long)level)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H24lEnumCastRow>()
            .Where(r => r.Score == (long)level)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectsACapturedEnumCastToInt()
    {
        H24lServiceLevel level = H24lServiceLevel.Premium;
        using TestDatabase db = Seed();
        List<H24lEnumCastRow> local = Rows();

        List<int> expected = local
            .OrderBy(r => r.Id)
            .Select(r => r.Rank + (int)level)
            .ToList();

        List<int> actual = db.Table<H24lEnumCastRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Rank + (int)level)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24lEnumCastRow> Rows()
    {
        return
        [
            new H24lEnumCastRow { Id = 1, Rank = 1, Score = 1 },
            new H24lEnumCastRow { Id = 2, Rank = 2, Score = 2 },
            new H24lEnumCastRow { Id = 3, Rank = 3, Score = 3 }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<H24lEnumCastRow>().Schema.CreateTable();
        db.Table<H24lEnumCastRow>().AddRange(Rows());
        return db;
    }
}
