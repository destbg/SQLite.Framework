using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DowWindowRow")]
public class DowWindowRow
{
    [Key]
    public int Id { get; set; }

    public DateTime When { get; set; }
}

public class WindowValueComputedDayOfWeekOuterCompareTests
{
    private static List<DowWindowRow> Rows() =>
    [
        new DowWindowRow { Id = 1, When = new DateTime(2024, 1, 1) },
        new DowWindowRow { Id = 2, When = new DateTime(2024, 1, 2) },
        new DowWindowRow { Id = 3, When = new DateTime(2024, 1, 5) },
        new DowWindowRow { Id = 4, When = new DateTime(2024, 1, 7) },
        new DowWindowRow { Id = 5, When = new DateTime(2024, 1, 8) },
    ];

    [Fact]
    public void FirstValueDayOfWeekOuterWhereTextStorageMatchesLinq()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<DowWindowRow>().Schema.CreateTable();
        db.Table<DowWindowRow>().AddRange(Rows());

        DayOfWeek firstDow = Rows().OrderBy(r => r.Id).First().When.DayOfWeek;
        int expected = Rows().Select(r => firstDow).Count(d => d == DayOfWeek.Monday);
        Assert.Equal(5, expected);

        int actual = db.Table<DowWindowRow>()
            .Select(r => new
            {
                r.Id,
                Dw = SQLiteWindowFunctions.FirstValue(r.When.DayOfWeek)
                    .Over()
                    .OrderBy(r.Id)
                    .AsValue(),
            })
            .Where(x => x.Dw == DayOfWeek.Monday)
            .Count();

        Assert.Equal(expected, actual);
    }
}
