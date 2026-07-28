using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23qLookupRows")]
public class H23qLookupRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ValueSourceLargeRowCountTests
{
    [Fact]
    public void ValuesRangeOverSixHundredScalarsReadsEveryRow()
    {
        using TestDatabase db = new();
        List<int> values = [.. Enumerable.Range(1, 600)];

        List<int> expected = values.OrderBy(v => v).ToList();
        List<int> actual = db.ValuesRange(values).ToList().OrderBy(v => v).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ValuesRangeOverSixHundredRowsJoinsAgainstATable()
    {
        using TestDatabase db = new();
        db.Table<H23qLookupRow>().Schema.CreateTable();
        db.Table<H23qLookupRow>().AddRange(StoredRows());

        List<int> keys = [.. Enumerable.Range(1, 600)];

        List<string> expected = StoredRows()
            .Where(r => keys.Contains(r.Id))
            .OrderBy(r => r.Id)
            .Select(r => r.Name)
            .ToList();

        List<string> actual = (from key in db.ValuesRange(keys)
                join row in db.Table<H23qLookupRow>() on key equals row.Id
                orderby row.Id
                select row.Name)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23qLookupRow> StoredRows()
    {
        return
        [
            new H23qLookupRow { Id = 3, Name = "three" },
            new H23qLookupRow { Id = 550, Name = "five fifty" },
            new H23qLookupRow { Id = 900, Name = "nine hundred" }
        ];
    }
}
