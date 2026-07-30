using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25qDowWrites")]
public class H25qDowWrite
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }

    public DayOfWeek Dow { get; set; }
}

[Table("H25qDowCopySources")]
public class H25qDowCopySource
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

[Table("H25qDowCopyTargets")]
public class H25qDowCopyTarget
{
    [Key]
    public int Id { get; set; }

    public DayOfWeek Dow { get; set; }
}

public class ComputedDayOfWeekWrittenToTextStoredColumnTests
{
    [Fact]
    public void ExecuteUpdateSettingAColumnFromAComputedDayOfWeekStoresTheQueryableForm()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), nameof(ExecuteUpdateSettingAColumnFromAComputedDayOfWeekStoresTheQueryableForm));
        db.Table<H25qDowWrite>().Schema.CreateTable();
        db.Table<H25qDowWrite>().AddRange(WriteRows());

        db.Table<H25qDowWrite>().ExecuteUpdate(s => s.Set(r => r.Dow, r => r.When.DayOfWeek));

        List<int> expected = WriteRows()
            .Select(r => new H25qDowWrite { Id = r.Id, When = r.When, Dow = r.When.DayOfWeek })
            .Where(r => r.Dow == DayOfWeek.Monday)
            .Select(r => r.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<H25qDowWrite>()
            .Where(r => r.Dow == DayOfWeek.Monday)
            .Select(r => r.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InsertFromQueryWritingAComputedDayOfWeekStoresTheQueryableForm()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), nameof(InsertFromQueryWritingAComputedDayOfWeekStoresTheQueryableForm));
        db.Table<H25qDowCopySource>().Schema.CreateTable();
        db.Table<H25qDowCopyTarget>().Schema.CreateTable();
        db.Table<H25qDowCopySource>().AddRange(CopySources());

        db.Table<H25qDowCopyTarget>().InsertFromQuery(
            db.Table<H25qDowCopySource>().Select(s => new H25qDowCopyTarget { Id = s.Id, Dow = s.When.DayOfWeek }));

        List<int> expected = CopySources()
            .Select(s => new H25qDowCopyTarget { Id = s.Id, Dow = s.When.DayOfWeek })
            .Where(t => t.Dow == DayOfWeek.Monday)
            .Select(t => t.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal([1, 3], expected);

        List<int> actual = db.Table<H25qDowCopyTarget>()
            .Where(t => t.Dow == DayOfWeek.Monday)
            .Select(t => t.Id)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25qDowWrite> WriteRows()
    {
        return
        [
            new H25qDowWrite { Id = 1, When = new DateTime(2024, 1, 1, 9, 0, 0) },
            new H25qDowWrite { Id = 2, When = new DateTime(2024, 1, 2, 9, 0, 0) },
            new H25qDowWrite { Id = 3, When = new DateTime(2024, 1, 8, 9, 0, 0) }
        ];
    }

    private static List<H25qDowCopySource> CopySources()
    {
        return
        [
            new H25qDowCopySource { Id = 1, When = new DateTime(2024, 1, 1, 9, 0, 0) },
            new H25qDowCopySource { Id = 2, When = new DateTime(2024, 1, 2, 9, 0, 0) },
            new H25qDowCopySource { Id = 3, When = new DateTime(2024, 1, 8, 9, 0, 0) }
        ];
    }
}
