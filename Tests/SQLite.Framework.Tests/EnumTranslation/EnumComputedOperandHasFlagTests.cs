using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
public enum ComputedFlagKind
{
    None = 0,
    Alpha = 1,
    Beta = 2,
    Gamma = 4,
}

public class EnumComputedRow
{
    [Key]
    public int Id { get; set; }

    public ComputedFlagKind Kind { get; set; }
}

public class EnumComputedOperandHasFlagTests
{
    private static List<EnumComputedRow> Rows() =>
    [
        new() { Id = 1, Kind = ComputedFlagKind.Alpha | ComputedFlagKind.Beta },
        new() { Id = 2, Kind = ComputedFlagKind.Gamma },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<EnumComputedRow>().Schema.CreateTable();
        db.Table<EnumComputedRow>().AddRange(Rows());
        return db;
    }

    private static ComputedFlagKind FlagFor(int id)
    {
        return id == 1 ? ComputedFlagKind.Alpha : ComputedFlagKind.Beta;
    }

    [Fact]
    public void HasFlagStaticHelperOnXorComputedOperandInSelectProjects()
    {
        using TestDatabase db = Seed();

        List<bool> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Kind ^ ComputedFlagKind.Beta).HasFlag(FlagFor(r.Id)))
            .ToList();
        Assert.Equal([true, true], expected);

        List<bool> actual = db.Table<EnumComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => (r.Kind ^ ComputedFlagKind.Beta).HasFlag(FlagFor(r.Id)))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
