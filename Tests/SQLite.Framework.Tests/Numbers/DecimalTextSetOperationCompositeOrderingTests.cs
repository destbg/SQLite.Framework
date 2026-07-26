using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22gTariffRows")]
public class H22gTariffRow
{
    [Key]
    public int Id { get; set; }

    public int Grp { get; set; }

    public decimal Amount { get; set; }
}

public class DecimalTextSetOperationCompositeOrderingTests
{
    [Fact]
    public void ThenByADecimalKeyAfterUnionKeepsTheFirstKey()
    {
        using TestDatabase db = Seed();
        List<H22gTariffRow> local = Rows();

        List<int> expected = local.Where(r => r.Id != 4)
            .Union(local.Where(r => r.Id == 4))
            .OrderBy(r => r.Grp)
            .ThenBy(r => r.Amount)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22gTariffRow>().Where(r => r.Id != 4)
            .Union(db.Table<H22gTariffRow>().Where(r => r.Id == 4))
            .OrderBy(r => r.Grp)
            .ThenBy(r => r.Amount)
            .AsEnumerable()
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ThenByADecimalKeyAfterConcatKeepsTheFirstKey()
    {
        using TestDatabase db = Seed();
        List<H22gTariffRow> local = Rows();

        List<int> expected = local.Where(r => r.Id != 4)
            .Concat(local.Where(r => r.Id == 4))
            .OrderBy(r => r.Grp)
            .ThenBy(r => r.Amount)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22gTariffRow>().Where(r => r.Id != 4)
            .Concat(db.Table<H22gTariffRow>().Where(r => r.Id == 4))
            .OrderBy(r => r.Grp)
            .ThenBy(r => r.Amount)
            .AsEnumerable()
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ThenByDescendingADecimalKeyAfterUnionKeepsTheFirstKey()
    {
        using TestDatabase db = Seed();
        List<H22gTariffRow> local = Rows();

        List<int> expected = local.Where(r => r.Id != 4)
            .Union(local.Where(r => r.Id == 4))
            .OrderBy(r => r.Grp)
            .ThenByDescending(r => r.Amount)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22gTariffRow>().Where(r => r.Id != 4)
            .Union(db.Table<H22gTariffRow>().Where(r => r.Id == 4))
            .OrderBy(r => r.Grp)
            .ThenByDescending(r => r.Amount)
            .AsEnumerable()
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22gTariffRow> Rows()
    {
        return
        [
            new H22gTariffRow { Id = 1, Grp = 1, Amount = 9.99m },
            new H22gTariffRow { Id = 2, Grp = 1, Amount = 10.11m },
            new H22gTariffRow { Id = 3, Grp = 2, Amount = 5.00m },
            new H22gTariffRow { Id = 4, Grp = 2, Amount = 20.00m }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<H22gTariffRow>().Schema.CreateTable();
        db.Table<H22gTariffRow>().AddRange(Rows());
        return db;
    }
}
