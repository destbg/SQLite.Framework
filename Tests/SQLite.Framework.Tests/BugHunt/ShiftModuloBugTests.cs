using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal sealed class ShiftRow
{
    [Key]
    public int Id { get; set; }

    public int N { get; set; }
}

internal sealed class ModuloRow
{
    [Key]
    public int Id { get; set; }

    public double D { get; set; }
}

public class ShiftModuloBugTests
{
    [Fact]
    public void RightShift_CountMasking_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<ShiftRow>().Schema.CreateTable();
        db.Table<ShiftRow>().Add(new ShiftRow { Id = 1, N = 1024 });

        int[] seed = [1024];
        int expected = seed.Select(n => n >> 33).First();
        int actual = db.Table<ShiftRow>().Select(x => x.N >> 33).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LeftShift_IntWidthAndSign_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<ShiftRow>().Schema.CreateTable();
        db.Table<ShiftRow>().Add(new ShiftRow { Id = 1, N = 1 });

        int[] seed = [1];
        List<int> expected = seed.Where(n => (n << 31) < 0).ToList();
        List<int> actual = db.Table<ShiftRow>().Where(x => (x.N << 31) < 0).Select(x => x.N).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DoubleModulo_LargeOperands_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<ModuloRow>().Schema.CreateTable();
        db.Table<ModuloRow>().Add(new ModuloRow { Id = 1, D = 1e20 });
        db.Table<ModuloRow>().Add(new ModuloRow { Id = 2, D = 1e19 });

        double expected1 = 1e20 % 3.0;
        double expected2 = 1e19 % 7.0;
        double actual1 = db.Table<ModuloRow>().Where(x => x.Id == 1).Select(x => x.D % 3.0).First();
        double actual2 = db.Table<ModuloRow>().Where(x => x.Id == 2).Select(x => x.D % 7.0).First();

        Assert.Equal(expected1, actual1);
        Assert.Equal(expected2, actual2);
    }
}
