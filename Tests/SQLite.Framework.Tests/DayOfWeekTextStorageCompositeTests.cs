using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DowMixEntry")]
public class DowMixEntryRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public DateTime Other { get; set; }

    public bool Flag { get; set; }

    public DayOfWeek Dow { get; set; }

    public DayOfWeek? NullableDow { get; set; }
}

[Table("DowSlotEntry")]
public class DowSlotEntryRow
{
    [Key]
    public int Id { get; set; }

    public DayOfWeek Dow { get; set; }
}

public class DayOfWeekTextStorageCompositeTests
{
    private static List<DowMixEntryRow> MixRows()
    {
        return
        [
            new DowMixEntryRow { Id = 1, When = new DateTime(2026, 7, 6), Other = new DateTime(2026, 7, 10), Flag = true, Dow = DayOfWeek.Monday, NullableDow = null },
            new DowMixEntryRow { Id = 2, When = new DateTime(2026, 7, 10), Other = new DateTime(2026, 7, 6), Flag = false, Dow = DayOfWeek.Monday, NullableDow = DayOfWeek.Wednesday },
            new DowMixEntryRow { Id = 3, When = new DateTime(2026, 7, 5), Other = new DateTime(2026, 7, 7), Flag = true, Dow = DayOfWeek.Friday, NullableDow = DayOfWeek.Monday },
            new DowMixEntryRow { Id = 4, When = new DateTime(2026, 7, 7), Other = new DateTime(2026, 7, 5), Flag = false, Dow = DayOfWeek.Tuesday, NullableDow = null },
        ];
    }

    private static List<DowSlotEntryRow> SlotRows()
    {
        return
        [
            new DowSlotEntryRow { Id = 1, Dow = DayOfWeek.Monday },
            new DowSlotEntryRow { Id = 2, Dow = DayOfWeek.Friday },
            new DowSlotEntryRow { Id = 3, Dow = DayOfWeek.Sunday },
        ];
    }

    private static TestDatabase CreateDatabase(out List<DowMixEntryRow> rows, out List<DowSlotEntryRow> slots, string methodName)
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), methodName);
        db.Table<DowMixEntryRow>().Schema.CreateTable();
        db.Table<DowSlotEntryRow>().Schema.CreateTable();
        rows = MixRows();
        slots = SlotRows();
        db.Table<DowMixEntryRow>().AddRange(rows);
        db.Table<DowSlotEntryRow>().AddRange(slots);
        return db;
    }

    [Fact]
    public void RelationalCompareAgainstACapturedVariableMatchesLinq()
    {
        using TestDatabase db = CreateDatabase(out List<DowMixEntryRow> rows, out _, nameof(RelationalCompareAgainstACapturedVariableMatchesLinq));
        DayOfWeek limit = DayOfWeek.Friday;

        List<int> expected = rows.Where(r => r.When.DayOfWeek < limit).OrderBy(r => r.Id).Select(r => r.Id).ToList();
        List<int> actual = db.Table<DowMixEntryRow>().Where(r => r.When.DayOfWeek < limit).OrderBy(r => r.Id).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CoalescedDayOfWeekEqualityMatchesLinq()
    {
        using TestDatabase db = CreateDatabase(out List<DowMixEntryRow> rows, out _, nameof(CoalescedDayOfWeekEqualityMatchesLinq));

        List<int> expected = rows.Where(r => (r.NullableDow ?? r.When.DayOfWeek) == DayOfWeek.Monday).OrderBy(r => r.Id).Select(r => r.Id).ToList();
        List<int> actual = db.Table<DowMixEntryRow>().Where(r => (r.NullableDow ?? r.When.DayOfWeek) == DayOfWeek.Monday).OrderBy(r => r.Id).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ContainsWithATernaryItemMatchesLinq()
    {
        using TestDatabase db = CreateDatabase(out List<DowMixEntryRow> rows, out _, nameof(ContainsWithATernaryItemMatchesLinq));
        DayOfWeek[] days = [DayOfWeek.Monday, DayOfWeek.Friday];

        List<int> expected = rows.Where(r => days.Contains(r.Flag ? r.When.DayOfWeek : r.Other.DayOfWeek)).OrderBy(r => r.Id).Select(r => r.Id).ToList();
        List<int> actual = db.Table<DowMixEntryRow>().Where(r => days.Contains(r.Flag ? r.When.DayOfWeek : r.Other.DayOfWeek)).OrderBy(r => r.Id).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EqualsWithATernaryReceiverMatchesLinq()
    {
        using TestDatabase db = CreateDatabase(out List<DowMixEntryRow> rows, out _, nameof(EqualsWithATernaryReceiverMatchesLinq));

        List<int> expected = rows.Where(r => (r.Flag ? r.When.DayOfWeek : DayOfWeek.Friday).Equals(r.Dow)).OrderBy(r => r.Id).Select(r => r.Id).ToList();
        List<int> actual = db.Table<DowMixEntryRow>().Where(r => (r.Flag ? r.When.DayOfWeek : DayOfWeek.Friday).Equals(r.Dow)).OrderBy(r => r.Id).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinOnATernaryKeyMatchesLinq()
    {
        using TestDatabase db = CreateDatabase(out List<DowMixEntryRow> rows, out List<DowSlotEntryRow> slots, nameof(JoinOnATernaryKeyMatchesLinq));

        var expected = rows
            .Join(slots, m => m.Flag ? m.When.DayOfWeek : m.Other.DayOfWeek, s => s.Dow, (m, s) => new { m.Id, SlotId = s.Id })
            .OrderBy(x => x.Id).ThenBy(x => x.SlotId).ToList();
        var actual = db.Table<DowMixEntryRow>()
            .Join(db.Table<DowSlotEntryRow>(), m => m.Flag ? m.When.DayOfWeek : m.Other.DayOfWeek, s => s.Dow, (m, s) => new { m.Id, SlotId = s.Id })
            .ToList().OrderBy(x => x.Id).ThenBy(x => x.SlotId).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinOnAProjectedComputedKeyMatchesLinq()
    {
        using TestDatabase db = CreateDatabase(out List<DowMixEntryRow> rows, out List<DowSlotEntryRow> slots, nameof(JoinOnAProjectedComputedKeyMatchesLinq));

        var expected = rows
            .Select(m => new { m.Id, D = m.When.DayOfWeek })
            .Join(slots, x => x.D, s => s.Dow, (x, s) => new { x.Id, SlotId = s.Id })
            .OrderBy(x => x.Id).ThenBy(x => x.SlotId).ToList();
        var actual = db.Table<DowMixEntryRow>()
            .Select(m => new { m.Id, D = m.When.DayOfWeek })
            .Join(db.Table<DowSlotEntryRow>(), x => x.D, s => s.Dow, (x, s) => new { x.Id, SlotId = s.Id })
            .ToList().OrderBy(x => x.Id).ThenBy(x => x.SlotId).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IntersectOfComputedAndStoredValuesMatchesLinq()
    {
        using TestDatabase db = CreateDatabase(out List<DowMixEntryRow> rows, out List<DowSlotEntryRow> slots, nameof(IntersectOfComputedAndStoredValuesMatchesLinq));

        List<DayOfWeek> expected = rows.Select(m => m.When.DayOfWeek).Intersect(slots.Select(s => s.Dow)).OrderBy(x => x).ToList();
        List<DayOfWeek> actual = db.Table<DowMixEntryRow>().Select(m => m.When.DayOfWeek)
            .Intersect(db.Table<DowSlotEntryRow>().Select(s => s.Dow))
            .ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnionOfComputedAndStoredValuesMatchesLinq()
    {
        using TestDatabase db = CreateDatabase(out List<DowMixEntryRow> rows, out List<DowSlotEntryRow> slots, nameof(UnionOfComputedAndStoredValuesMatchesLinq));

        List<DayOfWeek> expected = rows.Select(m => m.When.DayOfWeek).Union(slots.Select(s => s.Dow)).OrderBy(x => x).ToList();
        List<DayOfWeek> actual = db.Table<DowMixEntryRow>().Select(m => m.When.DayOfWeek)
            .Union(db.Table<DowSlotEntryRow>().Select(s => s.Dow))
            .ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SubqueryContainsAComputedDayOfWeekMatchesLinq()
    {
        using TestDatabase db = CreateDatabase(out List<DowMixEntryRow> rows, out List<DowSlotEntryRow> slots, nameof(SubqueryContainsAComputedDayOfWeekMatchesLinq));

        List<int> expected = rows.Where(m => slots.Select(s => s.Dow).Contains(m.When.DayOfWeek)).OrderBy(m => m.Id).Select(m => m.Id).ToList();
        List<int> actual = db.Table<DowMixEntryRow>()
            .Where(m => db.Table<DowSlotEntryRow>().Select(s => s.Dow).Contains(m.When.DayOfWeek))
            .OrderBy(m => m.Id).Select(m => m.Id).ToList();

        Assert.Equal(expected, actual);
    }
}
