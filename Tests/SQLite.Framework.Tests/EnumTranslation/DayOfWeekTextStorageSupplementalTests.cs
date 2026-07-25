using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DayOfWeekTextStorageSupplementalTests
{
    private static TestDatabase Seed(out List<DowMixEntryRow> rows, out List<DowSlotEntryRow> slots, string methodName)
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), methodName);
        db.Table<DowMixEntryRow>().Schema.CreateTable();
        db.Table<DowSlotEntryRow>().Schema.CreateTable();
        rows =
        [
            new DowMixEntryRow { Id = 1, When = new DateTime(2026, 7, 6), Other = new DateTime(2026, 7, 10), Flag = true, Dow = DayOfWeek.Monday, NullableDow = null },
            new DowMixEntryRow { Id = 2, When = new DateTime(2026, 7, 10), Other = new DateTime(2026, 7, 6), Flag = false, Dow = DayOfWeek.Monday, NullableDow = DayOfWeek.Wednesday },
        ];
        slots =
        [
            new DowSlotEntryRow { Id = 1, Dow = DayOfWeek.Monday },
            new DowSlotEntryRow { Id = 2, Dow = DayOfWeek.Friday },
        ];
        db.Table<DowMixEntryRow>().AddRange(rows);
        db.Table<DowSlotEntryRow>().AddRange(slots);
        return db;
    }

    [Fact]
    public void UnionOfStoredAndComputedValuesMatchesLinq()
    {
        using TestDatabase db = Seed(out List<DowMixEntryRow> rows, out List<DowSlotEntryRow> slots, nameof(UnionOfStoredAndComputedValuesMatchesLinq));

        List<DayOfWeek> expected = slots.Select(s => s.Dow).Union(rows.Select(m => m.When.DayOfWeek)).OrderBy(x => x).ToList();
        List<DayOfWeek> actual = db.Table<DowSlotEntryRow>().Select(s => s.Dow)
            .Union(db.Table<DowMixEntryRow>().Select(m => m.When.DayOfWeek))
            .ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnionOfPlainColumnsUnderTextStorageMatchesLinq()
    {
        using TestDatabase db = Seed(out List<DowMixEntryRow> rows, out List<DowSlotEntryRow> slots, nameof(UnionOfPlainColumnsUnderTextStorageMatchesLinq));

        List<int> expected = rows.Select(m => m.Id).Union(slots.Select(s => s.Id)).OrderBy(x => x).ToList();
        List<int> actual = db.Table<DowMixEntryRow>().Select(m => m.Id)
            .Union(db.Table<DowSlotEntryRow>().Select(s => s.Id))
            .ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctComputedScalarThenFilterMatchesLinq()
    {
        using TestDatabase db = Seed(out List<DowMixEntryRow> rows, out _, nameof(DistinctComputedScalarThenFilterMatchesLinq));

        List<bool> expected = rows.Select(m => m.When.DayOfWeek).Distinct()
            .Select(d => d == DayOfWeek.Monday).OrderBy(x => x).ToList();
        List<bool> actual = db.Table<DowMixEntryRow>().Select(m => m.When.DayOfWeek).Distinct()
            .Select(d => d == DayOfWeek.Monday).ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctComputedProjectionThenFilterMatchesLinq()
    {
        using TestDatabase db = Seed(out List<DowMixEntryRow> rows, out _, nameof(DistinctComputedProjectionThenFilterMatchesLinq));

        List<DayOfWeek> expected = rows.Select(m => new { m.Id, D = m.When.DayOfWeek }).Distinct()
            .Select(x => x.D).OrderBy(x => x).ToList();
        List<DayOfWeek> actual = db.Table<DowMixEntryRow>().Select(m => new { m.Id, D = m.When.DayOfWeek }).Distinct()
            .Select(x => x.D).ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnionOfProjectionsPairingStoredAndComputedMatchesLinq()
    {
        using TestDatabase db = Seed(out List<DowMixEntryRow> rows, out _, nameof(UnionOfProjectionsPairingStoredAndComputedMatchesLinq));

        var expected = rows.Select(m => new { m.Id, D = m.Dow })
            .Union(rows.Select(m => new { m.Id, D = m.When.DayOfWeek }))
            .OrderBy(x => x.Id).ThenBy(x => x.D).ToList();
        var actual = db.Table<DowMixEntryRow>().Select(m => new { m.Id, D = m.Dow })
            .Union(db.Table<DowMixEntryRow>().Select(m => new { m.Id, D = m.When.DayOfWeek }))
            .ToList().OrderBy(x => x.Id).ThenBy(x => x.D).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnionOfNullableProjectionsPairingStoredAndComputedMatchesLinq()
    {
        using TestDatabase db = Seed(out List<DowMixEntryRow> rows, out _, nameof(UnionOfNullableProjectionsPairingStoredAndComputedMatchesLinq));

        var expected = rows.Select(m => new { m.Id, D = m.NullableDow })
            .Union(rows.Select(m => new { m.Id, D = (DayOfWeek?)m.When.DayOfWeek }))
            .OrderBy(x => x.Id).ThenBy(x => x.D).ToList();
        var actual = db.Table<DowMixEntryRow>().Select(m => new { m.Id, D = m.NullableDow })
            .Union(db.Table<DowMixEntryRow>().Select(m => new { m.Id, D = (DayOfWeek?)m.When.DayOfWeek }))
            .ToList().OrderBy(x => x.Id).ThenBy(x => x.D).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SubqueryContainsWithAFilterParameterMatchesLinq()
    {
        using TestDatabase db = Seed(out List<DowMixEntryRow> rows, out List<DowSlotEntryRow> slots, nameof(SubqueryContainsWithAFilterParameterMatchesLinq));

        int floor = 0;
        List<int> expected = rows
            .Where(m => slots.Where(s => s.Id > floor).Select(s => s.Dow).Contains(m.When.DayOfWeek))
            .Select(m => m.Id).OrderBy(x => x).ToList();
        List<int> actual = db.Table<DowMixEntryRow>()
            .Where(m => db.Table<DowSlotEntryRow>().Where(s => s.Id > floor).Select(s => s.Dow).Contains(m.When.DayOfWeek))
            .Select(m => m.Id).ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnionOfComputedThenStoredValuesMatchesLinq()
    {
        using TestDatabase db = Seed(out List<DowMixEntryRow> rows, out List<DowSlotEntryRow> slots, nameof(UnionOfComputedThenStoredValuesMatchesLinq));

        List<DayOfWeek> expected = rows.Select(m => m.When.DayOfWeek).Union(slots.Select(s => s.Dow)).OrderBy(x => x).ToList();
        List<DayOfWeek> actual = db.Table<DowMixEntryRow>().Select(m => m.When.DayOfWeek)
            .Union(db.Table<DowSlotEntryRow>().Select(s => s.Dow))
            .ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CoalesceProjectionOfComputedOverComputedMatchesLinq()
    {
        using TestDatabase db = Seed(out List<DowMixEntryRow> rows, out _, nameof(CoalesceProjectionOfComputedOverComputedMatchesLinq));

        List<DayOfWeek> expected = rows
            .Select(m => (m.Flag ? m.When.DayOfWeek : (DayOfWeek?)null) ?? m.Other.DayOfWeek)
            .OrderBy(x => x).ToList();
        List<DayOfWeek> actual = db.Table<DowMixEntryRow>()
            .Select(m => (m.Flag ? m.When.DayOfWeek : (DayOfWeek?)null) ?? m.Other.DayOfWeek)
            .ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CoalesceOfACastComputedValueOverAStoredColumnMatchesLinq()
    {
        using TestDatabase db = Seed(out List<DowMixEntryRow> rows, out _, nameof(CoalesceOfACastComputedValueOverAStoredColumnMatchesLinq));

        List<int> expected = rows
            .Where(m => ((m.Flag ? m.When.DayOfWeek : (DayOfWeek?)null) ?? m.Dow) == DayOfWeek.Monday)
            .OrderBy(m => m.Id).Select(m => m.Id).ToList();
        List<int> actual = db.Table<DowMixEntryRow>()
            .Where(m => ((m.Flag ? m.When.DayOfWeek : (DayOfWeek?)null) ?? m.Dow) == DayOfWeek.Monday)
            .OrderBy(m => m.Id).Select(m => m.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CastOfAComputedValueToIntMatchesLinq()
    {
        using TestDatabase db = Seed(out List<DowMixEntryRow> rows, out _, nameof(CastOfAComputedValueToIntMatchesLinq));

        List<int> expected = rows.Select(m => (int)m.When.DayOfWeek).OrderBy(x => x).ToList();
        List<int> actual = db.Table<DowMixEntryRow>().Select(m => (int)m.When.DayOfWeek)
            .ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CoalesceOfTwoComputedValuesMatchesLinq()
    {
        using TestDatabase db = Seed(out List<DowMixEntryRow> rows, out _, nameof(CoalesceOfTwoComputedValuesMatchesLinq));

        List<int> expected = rows
            .Where(m => ((m.Flag ? m.When.DayOfWeek : (DayOfWeek?)null) ?? m.Other.DayOfWeek) == DayOfWeek.Monday)
            .OrderBy(m => m.Id).Select(m => m.Id).ToList();
        List<int> actual = db.Table<DowMixEntryRow>()
            .Where(m => ((m.Flag ? m.When.DayOfWeek : (DayOfWeek?)null) ?? m.Other.DayOfWeek) == DayOfWeek.Monday)
            .OrderBy(m => m.Id).Select(m => m.Id).ToList();

        Assert.Equal(expected, actual);
    }
}
