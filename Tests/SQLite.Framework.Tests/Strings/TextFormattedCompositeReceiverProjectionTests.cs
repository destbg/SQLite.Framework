using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("TextFmtComposite")]
public class TextFmtCompositeRow
{
    [Key]
    public int Id { get; set; }

    public bool Flag { get; set; }

    public DateTime When { get; set; }

    public DateTime Other { get; set; }
}

public class TextFormattedCompositeReceiverProjectionTests
{
    [Fact]
    public void TernaryReceiverAddDaysMatchesLinqToObjects()
    {
        using TestDatabase db = new(b => b.UseDateTimeStorage(DateTimeStorageMode.TextFormatted));
        db.Table<TextFmtCompositeRow>().Schema.CreateTable();
        List<TextFmtCompositeRow> mem =
        [
            new() { Id = 1, Flag = true, When = new DateTime(2024, 1, 1, 10, 0, 0), Other = new DateTime(2024, 2, 2, 11, 0, 0) },
            new() { Id = 2, Flag = false, When = new DateTime(2024, 3, 3, 12, 0, 0), Other = new DateTime(2024, 4, 4, 13, 0, 0) },
        ];
        foreach (TextFmtCompositeRow row in mem)
        {
            db.Table<TextFmtCompositeRow>().Add(row);
        }

        List<DateTime> expected = mem.OrderBy(r => r.Id).Select(r => (r.Flag ? r.When : r.Other).AddDays(1)).ToList();
        List<DateTime> actual = db.Table<TextFmtCompositeRow>().OrderBy(r => r.Id).Select(r => (r.Flag ? r.When : r.Other).AddDays(1)).ToList();

        Assert.Equal(expected, actual);
    }
}
