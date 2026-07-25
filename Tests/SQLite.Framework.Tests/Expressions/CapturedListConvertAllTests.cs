using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ConvertAllRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public class CapturedListConvertAllTests
{
    [Fact]
    public void CapturedListConvertAllInSelectRunsInMemory()
    {
        using TestDatabase db = new();
        db.Table<ConvertAllRow>().Schema.CreateTable();
        db.Table<ConvertAllRow>().Add(new ConvertAllRow { Id = 1, Amount = 3 });
        List<int> values = [1, 2];

        List<ConvertAllRow> memory = [new ConvertAllRow { Id = 1, Amount = 3 }];
        List<int> expected = memory.Select(r => values.ConvertAll(v => v + r.Amount)).First();
        Assert.Equal([4, 5], expected);

        List<int> actual = db.Table<ConvertAllRow>().Select(r => values.ConvertAll(v => v + r.Amount)).First();
        Assert.Equal(expected, actual);
    }
}
