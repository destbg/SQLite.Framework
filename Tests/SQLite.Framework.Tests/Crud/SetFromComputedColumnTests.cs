using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SetFromComputedRows")]
public class SetFromComputedRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }

    public int Doubled { get; set; }

    public int Copy { get; set; }
}

public class SetFromComputedColumnTests
{
    [Fact]
    public void SetFromAComputedColumnCopiesItsValue()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<SetFromComputedRow>().Computed(r => r.Doubled, r => r.Amount * 2));
        db.Table<SetFromComputedRow>().Schema.CreateTable();
        db.Table<SetFromComputedRow>().AddRange(
        [
            new SetFromComputedRow { Id = 1, Amount = 10 },
            new SetFromComputedRow { Id = 2, Amount = 20 }
        ]);

        db.Table<SetFromComputedRow>().ExecuteUpdate(s => s.Set(r => r.Copy, r => r.Doubled));

        List<(int Doubled, int Copy)> actual = db.Table<SetFromComputedRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Doubled, r.Copy })
            .ToList()
            .Select(r => (r.Doubled, r.Copy))
            .ToList();

        Assert.Equal([(20, 20), (40, 40)], actual);
    }
}
