using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MoneyRows")]
file sealed class MoneyRow
{
    [Key]
    public int Id { get; set; }

    public decimal? Amount { get; set; }
}

public class PagingSetOpTests
{
    [Fact]
    public void NullableDecimalValueKeepsCastUnderTextStorage()
    {
        using TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<MoneyRow>().Schema.CreateTable();
        db.Table<MoneyRow>().Add(new MoneyRow { Id = 1, Amount = 9.00m });
        db.Table<MoneyRow>().Add(new MoneyRow { Id = 2, Amount = 15.00m });
        db.Table<MoneyRow>().Add(new MoneyRow { Id = 3, Amount = null });

        List<int> ids = db.Table<MoneyRow>()
            .Where(m => m.Amount.Value > 10.00m)
            .OrderBy(m => m.Id)
            .Select(m => m.Id)
            .ToList();

        Assert.Equal(new[] { 2 }, ids);
    }

    [Fact]
    public void FirstAfterTakeZeroThrows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        Assert.Throws<InvalidOperationException>(() => db.Table<Book>().Take(0).First());
    }

    [Fact]
    public void ElementAtAfterTakeRespectsTheCap()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 1; i <= 8; i++)
        {
            db.Table<Book>().Add(new Book { Id = i, Title = "a", AuthorId = 1, Price = i });
        }

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Enumerable.Range(1, 8).Take(3).ElementAt(5));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => db.Table<Book>().OrderBy(x => x.Id).Take(3).ElementAt(5));
    }

}
